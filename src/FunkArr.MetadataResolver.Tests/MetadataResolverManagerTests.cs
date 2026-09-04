using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.MetadataResolver;
using FunkArr.Tests.Shared;

namespace FunkArr.MetadataResolver.Tests;

public sealed class MetadataResolverManagerTests : TestKit
{
    private static TvdbClient CreateUnconfiguredTvdbClient()
    {
        var factory = new TestHttpClientFactory();
        var options = new TestOptionsMonitor<TvdbOptions>(new TvdbOptions { ApiKey = "" });
        return new TvdbClient(factory, options);
    }

    private static TmdbClient CreateUnconfiguredTmdbClient()
    {
        var factory = new TestHttpClientFactory();
        var options = new TestOptionsMonitor<TmdbOptions>(new TmdbOptions { ApiKey = "" });
        return new TmdbClient(factory, options);
    }

    private IActorRef CreateManager() =>
        Sys.ActorOf(Props.Create(() =>
            new MetadataResolverManager(CreateUnconfiguredTvdbClient(), CreateUnconfiguredTmdbClient())));

    [Fact]
    public void Responds_with_failure_when_tvdb_not_configured()
    {
        var manager = CreateManager();

        var candidates = new[]
        {
            new EpisodeCandidate(0, "Roomservice", "Roomservice", null, 5400, null, null),
        };

        manager.Tell(new ResolveEpisodes(83214, 2026, new ResolutionConfig(), candidates));

        var response = ExpectMsg<EpisodeResolutionFailed>();
        Assert.Contains("not configured", response.Reason);
    }

    [Fact]
    public void Responds_with_failure_when_tmdb_not_configured()
    {
        var manager = CreateManager();

        var candidates = new[]
        {
            new MovieCandidate(0, "Test Film", null, 5400),
        };

        manager.Tell(new ResolveMovie(null, 550, candidates));

        var response = ExpectMsg<MovieResolutionFailed>();
        Assert.Contains("not configured", response.Reason);
    }

    [Fact]
    public void Responds_with_empty_resolved_when_strategy_is_none()
    {
        var manager = CreateManager();

        var candidates = new[]
        {
            new EpisodeCandidate(0, "Test", null, null, 5400, null, null),
        };

        manager.Tell(new ResolveEpisodes(83214, null, new ResolutionConfig("none"), candidates));

        var response = ExpectMsg<EpisodesResolved>();
        Assert.Empty(response.Episodes);
    }

    [Fact]
    public void Cache_stats_returns_zero_when_empty()
    {
        var manager = CreateManager();

        manager.Tell(new QueryCacheStats());

        var stats = ExpectMsg<CacheStatsResult>();
        Assert.Equal(0, stats.TvdbEntries);
        Assert.Equal(0, stats.TmdbEntries);
        Assert.Null(stats.OldestEntry);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
