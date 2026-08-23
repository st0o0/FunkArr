using System.Collections.Concurrent;
using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Search;

public sealed class SearchCoordinatorTests : Akka.Hosting.TestKit.TestKit
{
    private static readonly JsonSerializerOptions MediathekJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private ConcurrentQueue<MatchRecord>? _recordedMatches;
    private Func<int?, IReadOnlyList<Rule>>? _rulesProvider;

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        _recordedMatches = new ConcurrentQueue<MatchRecord>();
        _rulesProvider = _ => [];

        builder.WithActors((system, registry, _) =>
        {
            var ruleSetStub = system.ActorOf(Props.Create(() =>
                new StubRuleSetRegistry(
                    request => _rulesProvider!(request.TvdbId),
                    record => _recordedMatches!.Enqueue(record))));
            registry.Register<RuleSetCoordinator>(ruleSetStub);
        });
    }

    private static MediathekClient CreateMediathekClient(MediathekResultItem[] items, Action? onSend = null)
    {
        var response = new MediathekQueryResponse { Result = new MediathekQueryResult { Results = items } };
        var json = JsonSerializer.Serialize(response, MediathekJsonOptions);
        var handler = new FakeHttpMessageHandler(_ =>
        {
            onSend?.Invoke();
            return FakeHttpMessageHandler.JsonResponse(json);
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://mediathekviewweb.de/") };
        return new MediathekClient(httpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<MediathekClient>.Instance);
    }

    private static TvdbClient CreateTvdbClient() =>
        new(new HttpClient(new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.JsonResponse("{}")))
        { BaseAddress = new Uri("https://api.thetvdb.com/") });

    private static TmdbClient CreateTmdbClient() =>
        new(new HttpClient(new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.JsonResponse("{}")))
        { BaseAddress = new Uri("https://api.themoviedb.org/3/") },
            Options.Create(new SearchOptions()));

    private static QualityProbeService CreateQualityProbeService()
    {
        var opts = new QualityOptions { Probing = false };
        var factory = new NoOpHttpClientFactory();
        return new QualityProbeService(factory, NullLogger<QualityProbeService>.Instance, Options.Create(opts));
    }

    private IActorRef CreateActor(MediathekClient mediathekClient, TvdbClient? tvdbClient = null)
    {
        var options = Options.Create(new SearchOptions { QualityProbeLimit = 30 });
        var qualityProbeService = CreateQualityProbeService();
        var tvdb = tvdbClient ?? CreateTvdbClient();
        var tmdb = CreateTmdbClient();
        return Sys.ActorOf(Props.Create(() => new SearchCoordinator(mediathekClient, tvdb, tmdb, qualityProbeService, options)));
    }

    [Fact(Timeout = 5000)]
    public async Task TvSearch_CacheMissRoutesToChildAndCachesReply()
    {
        var item = new MediathekResultItem
        {
            Title = "Tatort Folge",
            Topic = "Tatort",
            Channel = "ARD",
            Duration = 2700,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url_Video_HD = "https://example.com/hd.mp4",
        };
        var callCount = 0;
        var mediathek = CreateMediathekClient([item], () => Interlocked.Increment(ref callCount));
        var actor = CreateActor(mediathek);

        var request = new SearchCoordinator.TvSearchRequest(1, "Tatort", null, null, "Tatort");

        var first = await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));
        Assert.Single(first.Results);
        Assert.Equal(1, callCount);

        await AwaitAssertAsync(() => Assert.Single(_recordedMatches!));

        var second = await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));
        Assert.Single(second.Results);
        Assert.Equal(1, callCount);
    }

    [Fact(Timeout = 5000)]
    public async Task MovieSearch_CacheMissRoutesToChildAndCachesReply()
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
        var callCount = 0;
        var mediathek = CreateMediathekClient([item], () => Interlocked.Increment(ref callCount));
        var actor = CreateActor(mediathek);

        var request = new SearchCoordinator.MovieSearchRequest("tt1234567", "Der Film");

        var first = await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));
        Assert.Single(first.Results);
        Assert.Equal(1, callCount);

        var second = await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));
        Assert.Single(second.Results);
        Assert.Equal(1, callCount);
    }

    [Fact(Timeout = 5000)]
    public async Task TextSearch_CacheMissRoutesToChildAndCachesReply()
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
        var callCount = 0;
        var mediathek = CreateMediathekClient([item], () => Interlocked.Increment(ref callCount));
        var actor = CreateActor(mediathek);

        var request = new SearchCoordinator.TextSearchRequest("Beliebiger Titel");

        var first = await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));
        Assert.Single(first.Results);
        Assert.Equal(1, callCount);

        var second = await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));
        Assert.Single(second.Results);
        Assert.Equal(1, callCount);
    }

    [Fact(Timeout = 5000)]
    public async Task TvSearch_MatchRecordForwardedToMatchLedger()
    {
        var item = new MediathekResultItem
        {
            Title = "Tatort Folge",
            Topic = "Tatort",
            Channel = "ARD",
            Duration = 2700,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url_Video_HD = "https://example.com/hd.mp4",
        };
        var mediathek = CreateMediathekClient([item]);
        var actor = CreateActor(mediathek);

        var request = new SearchCoordinator.TvSearchRequest(1, "Tatort", null, null, "Tatort");
        await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));

        await AwaitAssertAsync(() =>
        {
            Assert.Single(_recordedMatches!);
            Assert.Equal("generic-pipeline", _recordedMatches!.Single().Source);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Requests_AreEventuallyServedAfterDependencyResolution()
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

        var request = new SearchCoordinator.TextSearchRequest("Beliebiger Titel");
        var response = await actor.Ask<SearchCoordinator.SearchResponse>(request, TimeSpan.FromSeconds(3));

        Assert.Single(response.Results);
    }

    private sealed class StubRuleSetRegistry : ReceiveActor
    {
        public StubRuleSetRegistry(
            Func<RuleSetCoordinator.GetRulesForTopic, IReadOnlyList<Rule>> rulesProvider,
            Action<MatchRecord> onRecord)
        {
            Receive<RuleSetCoordinator.GetRulesForTopic>(msg =>
                Sender.Tell(new RuleSetCoordinator.RulesResponse(rulesProvider(msg))));
            Receive<MatchQualityWorker.RecordMatchResult>(msg => onRecord(msg.Record));
        }
    }

    private sealed class NoOpHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.JsonResponse("{}")));
    }
}

public class MediathekQueryTests
{
    [Fact]
    public void MediathekQuery_CanBeConstructed()
    {
        var query = new MediathekQuery
        {
            Queries =
            [
                new MediathekQueryItem { Fields = ["topic", "title"], Query = "Tatort" },
            ],
        };

        Assert.Single(query.Queries);
        Assert.Equal("Tatort", query.Queries[0].Query);
        Assert.Equal("desc", query.SortOrder);
        Assert.Equal(5000, query.Size);
    }

    [Fact]
    public void MediathekResultItem_DefaultsToEmpty()
    {
        var item = new MediathekResultItem();

        Assert.Equal(string.Empty, item.Channel);
        Assert.Equal(string.Empty, item.Topic);
        Assert.Equal(string.Empty, item.Title);
        Assert.Equal(0, item.Duration);
    }

    [Fact]
    public void TvdbShowInfo_DefaultsToEmpty()
    {
        var info = new TvdbShowInfo();

        Assert.Equal(string.Empty, info.SeriesName);
        Assert.Empty(info.Aliases);
    }
}
