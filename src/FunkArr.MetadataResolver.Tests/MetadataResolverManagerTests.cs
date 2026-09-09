using Akka.Actor;
using Akka.DependencyInjection;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.MetadataResolver;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.MetadataResolver.Tests;

public sealed class MetadataResolverManagerTests : TestKit
{
    public MetadataResolverManagerTests()
        : base(CreateConfig())
    {
    }

    private static Akka.Actor.Setup.ActorSystemSetup CreateConfig()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TestOptionsMonitor<TvdbOptions>(new TvdbOptions { ApiKey = "" }));
        services.AddSingleton<Microsoft.Extensions.Options.IOptionsMonitor<TvdbOptions>>(sp =>
            sp.GetRequiredService<TestOptionsMonitor<TvdbOptions>>());
        services.AddSingleton(new TestOptionsMonitor<TmdbOptions>(new TmdbOptions { ApiKey = "" }));
        services.AddSingleton<Microsoft.Extensions.Options.IOptionsMonitor<TmdbOptions>>(sp =>
            sp.GetRequiredService<TestOptionsMonitor<TmdbOptions>>());
        services.AddMemoryCache();
        services.AddHttpClient<TvdbClient>();
        services.AddHttpClient<TmdbClient>();
        services.AddSingleton<EpisodeResolver>();
        services.AddSingleton<MovieResolver>();
        var provider = services.BuildServiceProvider();

        return Akka.Actor.Setup.ActorSystemSetup.Create(DependencyResolverSetup.Create(provider));
    }

    private IActorRef CreateManager()
    {
        var resolver = DependencyResolver.For(Sys);
        return Sys.ActorOf(resolver.Props<MetadataResolverManager>());
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
    public void Cache_stats_returns_counts()
    {
        var manager = CreateManager();

        manager.Tell(new QueryCacheStats());

        var stats = ExpectMsg<CacheStatsResult>();
        Assert.NotNull(stats);
    }

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
}
