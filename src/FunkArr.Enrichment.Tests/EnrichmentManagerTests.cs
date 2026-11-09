using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core.Configuration;
using FunkArr.Enrichment.Tmdb;
using FunkArr.Enrichment.Tvdb;
using FunkArr.Messages.Enrichment;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunkArr.Enrichment.Tests;

public sealed class EnrichmentManagerTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<IOptionsMonitor<TvdbOptions>>(
            new TestOptionsMonitor<TvdbOptions>(new TvdbOptions { ApiKey = "" }));
        services.AddSingleton<IOptionsMonitor<TmdbOptions>>(
            new TestOptionsMonitor<TmdbOptions>(new TmdbOptions { ApiKey = "" }));
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<TvdbClient>();
        services.AddHttpClient<TmdbClient>();
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
        {
            var manager = system.ActorOf(resolver.Props<EnrichmentManager>(), "enrichment-manager");
            registry.Register<EnrichmentManager>(manager);
        });
    }

    private IActorRef Manager => ActorRegistry.Get<EnrichmentManager>();

    [Fact]
    public void Responds_with_failure_when_tvdb_not_configured()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Roomservice", "Roomservice", null, 5400, null, null)
        };

        Manager.Tell(new EnrichEpisodes(83214, 2026, candidates));

        var response = ExpectMsg<EnrichEpisodesFailed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("not configured", response.Cause.Message);
    }

    [Fact]
    public void Responds_with_failure_when_tmdb_not_configured()
    {
        var candidates = new[]
        {
            new MovieCandidate(0, "Test Film", null, 5400)
        };

        Manager.Tell(new EnrichMovies(null, 550, candidates));

        var response = ExpectMsg<EnrichMoviesFailed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("not configured", response.Cause.Message);
    }
}
