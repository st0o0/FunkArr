using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Shared.Models;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Search;

public sealed class TvSearchActorTests : Akka.Hosting.TestKit.TestKit
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

    private static TvdbClient CreateTvdbClient(TvdbEpisodeInfo[] episodes, TvdbShowInfo? show = null)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/episodes/query"))
            {
                var episodesResponse = new TvdbApiResponse<TvdbEpisodeInfo[]> { Data = episodes };
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(episodesResponse));
            }

            var showResponse = new TvdbApiResponse<TvdbShowInfo> { Data = show ?? new TvdbShowInfo { SeriesName = "Show" } };
            return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(showResponse));
        });
        return new TvdbClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.thetvdb.com/") });
    }

    private static QualityProbeService CreateQualityProbeService()
    {
        var opts = new QualityOptions { Probing = false };
        var factory = new NoOpHttpClientFactory();
        return new QualityProbeService(factory, NullLogger<QualityProbeService>.Instance, Options.Create(opts));
    }

    private IActorRef CreateActor(MediathekClient mediathekClient, TvdbClient tvdbClient, int probeLimit = 30) =>
        Sys.ActorOf(Props.Create(() =>
            new TvSearchActor(mediathekClient, tvdbClient, CreateQualityProbeService(), probeLimit)));

    private static MediathekResultItem MakeItem(string title, string topic = "Tatort", int duration = 2700) =>
        new()
        {
            Title = title,
            Topic = topic,
            Channel = "ARD",
            Duration = duration,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url_Video_HD = "https://example.com/hd.mp4",
        };

    [Fact(Timeout = 5000)]
    public async Task RulesetPath_MatchesAndReturnsRulesetSource()
    {
        var item = MakeItem("Tatort S01E01");
        var mediathek = CreateMediathekClient([item]);
        var episode = new TvdbEpisodeInfo { AiredSeason = 1, AiredEpisodeNumber = 1, EpisodeName = "Pilot", FirstAired = "2020-01-01" };
        var tvdb = CreateTvdbClient([episode]);
        var actor = CreateActor(mediathek, tvdb);

        var rule = new Rule
        {
            Priority = 0,
            Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
            SeasonRegex = @"S(\d{2})E\d{2}",
            EpisodeRegex = @"S\d{2}E(\d{2})",
        };

        var request = new SearchActor.TvSearchRequest(1, "Tatort", 1, 1, "Tatort");
        var command = new ExecuteTvSearch("cache-key", request, "Tatort", "Tatort", [rule], TestActor);

        actor.Tell(command, TestActor);

        var completed = await ExpectMsgAsync<SearchCompleted>(TimeSpan.FromSeconds(3));

        Assert.Equal("cache-key", completed.CacheKey);
        Assert.Single(completed.Results);
        Assert.NotNull(completed.MatchRecord);
        Assert.Equal("ruleset", completed.MatchRecord!.Source);
        Assert.Single(completed.MatchRecord.Matched);
        Assert.Same(TestActor, completed.ReplyTo);
    }

    [Fact(Timeout = 5000)]
    public async Task GenericPath_NoRules_ResolvesAirDateAndReturnsGenericPipelineSource()
    {
        var item = MakeItem("Tatort - irgendein Titel");
        var mediathek = CreateMediathekClient([item]);
        var episode = new TvdbEpisodeInfo { AiredSeason = 1, AiredEpisodeNumber = 1, EpisodeName = "Pilot", FirstAired = "2020-01-01" };
        var tvdb = CreateTvdbClient([episode]);
        var actor = CreateActor(mediathek, tvdb);

        var request = new SearchActor.TvSearchRequest(1, "Tatort", 1, 1, "Tatort");
        var command = new ExecuteTvSearch("cache-key", request, "Tatort", "Tatort", [], TestActor);

        actor.Tell(command, TestActor);

        var completed = await ExpectMsgAsync<SearchCompleted>(TimeSpan.FromSeconds(3));

        Assert.NotNull(completed.MatchRecord);
        Assert.Equal("generic-pipeline", completed.MatchRecord!.Source);
        Assert.Empty(completed.MatchRecord.Matched);
    }

    [Fact(Timeout = 5000)]
    public async Task EmptyRules_FallsBackToGenericPipeline()
    {
        var item = MakeItem("Some Show Episode");
        var mediathek = CreateMediathekClient([item]);
        var tvdb = CreateTvdbClient([]);
        var actor = CreateActor(mediathek, tvdb);

        var request = new SearchActor.TvSearchRequest(0, "Some Show", null, null, "Some Show");
        var command = new ExecuteTvSearch("cache-key", request, "Some Show", "Some Show", [], TestActor);

        actor.Tell(command, TestActor);

        var completed = await ExpectMsgAsync<SearchCompleted>(TimeSpan.FromSeconds(3));

        Assert.Equal("generic-pipeline", completed.MatchRecord!.Source);
    }

    private sealed class NoOpHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.JsonResponse("{}")));
    }
}
