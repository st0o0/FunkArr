using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.Search;
using FunkArr.Shared.Models;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Search;

public sealed class MovieSearchActorTests : Akka.Hosting.TestKit.TestKit
{
    private static readonly JsonSerializerOptions MediathekJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    private static MediathekClient CreateMediathekClient(MediathekResultItem[] items)
    {
        var response = new MediathekQueryResponse { Result = items };
        var json = JsonSerializer.Serialize(response, MediathekJsonOptions);
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(json));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://mediathekviewweb.de/") };
        return new MediathekClient(httpClient);
    }

    private static QualityProbeService CreateQualityProbeService()
    {
        var opts = new QualityOptions { Probing = false };
        var factory = new NoOpHttpClientFactory();
        return new QualityProbeService(factory, NullLogger<QualityProbeService>.Instance, Options.Create(opts));
    }

    private IActorRef CreateActor(MediathekClient mediathekClient, int probeLimit = 30) =>
        Sys.ActorOf(Props.Create(() =>
            new MovieSearchActor(mediathekClient, CreateQualityProbeService(), probeLimit)));

    [Fact(Timeout = 5000)]
    public async Task ExecuteMovieSearch_ReturnsResultsAndGenericPipelineRecord()
    {
        var item = new MediathekResultItem
        {
            Title = "Der Film",
            Topic = "Der Film",
            Channel = "ZDF",
            Duration = 5400,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url_Video_HD = "https://example.com/hd.mp4",
        };
        var mediathek = CreateMediathekClient([item]);
        var actor = CreateActor(mediathek);

        var request = new SearchActor.MovieSearchRequest("tt1234567", "Der Film");
        var command = new ExecuteMovieSearch("movie-cache-key", request, "Der Film", TestActor);

        actor.Tell(command, TestActor);

        var completed = await ExpectMsgAsync<SearchCompleted>(TimeSpan.FromSeconds(3));

        Assert.Equal("movie-cache-key", completed.CacheKey);
        Assert.Single(completed.Results);
        Assert.Equal("Der Film", completed.Results[0].Title);
        Assert.NotNull(completed.MatchRecord);
        Assert.Equal("generic-pipeline", completed.MatchRecord!.Source);
        Assert.Equal("Der Film", completed.MatchRecord.SearchTopic);
        Assert.Null(completed.MatchRecord.TvdbId);
        Assert.Same(TestActor, completed.ReplyTo);
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteMovieSearch_NoMatchingResults_ReturnsEmptyWithRecord()
    {
        var item = new MediathekResultItem
        {
            Title = "Unrelated Title",
            Topic = "Unrelated Topic",
            Channel = "ARD",
            Duration = 5400,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url_Video_HD = "https://example.com/hd.mp4",
        };
        var mediathek = CreateMediathekClient([item]);
        var actor = CreateActor(mediathek);

        var request = new SearchActor.MovieSearchRequest(null, "Der Film");
        var command = new ExecuteMovieSearch("movie-cache-key", request, "Der Film", TestActor);

        actor.Tell(command, TestActor);

        var completed = await ExpectMsgAsync<SearchCompleted>(TimeSpan.FromSeconds(3));

        Assert.Empty(completed.Results);
        Assert.NotNull(completed.MatchRecord);
        Assert.Equal(1, completed.MatchRecord!.TotalResults);
    }

    private sealed class NoOpHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.JsonResponse("{}")));
    }
}
