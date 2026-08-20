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

public sealed class TextSearchActorTests : Akka.Hosting.TestKit.TestKit
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
            new TextSearchActor(mediathekClient, CreateQualityProbeService(), probeLimit)));

    [Fact(Timeout = 5000)]
    public async Task ExecuteTextSearch_ReturnsResultsAndGenericPipelineRecord()
    {
        var item = new MediathekResultItem
        {
            Title = "Beliebiger Titel",
            Topic = "Beliebiges Thema",
            Channel = "ARD",
            Duration = 1200,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url_Video_HD = "https://example.com/hd.mp4",
        };
        var mediathek = CreateMediathekClient([item]);
        var actor = CreateActor(mediathek);

        var request = new SearchActor.TextSearchRequest("Beliebiger Titel");
        var command = new ExecuteTextSearch("text-cache-key", request, TestActor);

        actor.Tell(command, TestActor);

        var completed = await ExpectMsgAsync<SearchCompleted>(TimeSpan.FromSeconds(3));

        Assert.Equal("text-cache-key", completed.CacheKey);
        Assert.Single(completed.Results);
        Assert.NotNull(completed.MatchRecord);
        Assert.Equal("generic-pipeline", completed.MatchRecord!.Source);
        Assert.Equal("Beliebiger Titel", completed.MatchRecord.SearchTopic);
        Assert.Same(TestActor, completed.ReplyTo);
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteTextSearch_NoResults_ReturnsEmptyWithRecord()
    {
        var mediathek = CreateMediathekClient([]);
        var actor = CreateActor(mediathek);

        var request = new SearchActor.TextSearchRequest("nothing");
        var command = new ExecuteTextSearch("text-cache-key", request, TestActor);

        actor.Tell(command, TestActor);

        var completed = await ExpectMsgAsync<SearchCompleted>(TimeSpan.FromSeconds(3));

        Assert.Empty(completed.Results);
        Assert.Equal(0, completed.MatchRecord!.TotalResults);
    }

    private sealed class NoOpHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.JsonResponse("{}")));
    }
}
