using Akka.Actor;
using Akka.DependencyInjection;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.Enrichment.Tests;

public sealed class EnrichmentManagerTests : TestKit
{
    public EnrichmentManagerTests()
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
        services.AddSingleton<EpisodeEnricher>();
        services.AddSingleton<MovieEnricher>();
        var provider = services.BuildServiceProvider();

        return Akka.Actor.Setup.ActorSystemSetup.Create(DependencyResolverSetup.Create(provider));
    }

    private IActorRef CreateManager()
    {
        var resolver = DependencyResolver.For(Sys);
        return Sys.ActorOf(resolver.Props<EnrichmentManager>());
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

        manager.Tell(new EnrichEpisodes(83214, 2026, candidates));

        var response = ExpectMsg<EnrichEpisodesFailed>();
        Assert.NotNull(response);
    }

    [Fact]
    public void Responds_with_failure_when_tmdb_not_configured()
    {
        var manager = CreateManager();

        var candidates = new[]
        {
            new MovieCandidate(0, "Test Film", null, 5400),
        };

        manager.Tell(new EnrichMovies(null, 550, candidates));

        var response = ExpectMsg<EnrichMoviesFailed>();
        Assert.Contains("not configured", response.Cause.Message);
    }
}
