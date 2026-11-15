using System.Net;
using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core.Configuration;
using FunkArr.Enrichment.Tvdb;
using FunkArr.Messages.Enrichment;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunkArr.Enrichment.Tests.Tvdb;

public sealed class TvdbEnrichmentActorTests : Akka.Hosting.TestKit.TestKit
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly TvdbEpisode[] _allEpisodes =
    [
        new(2025, 1, "Old Season Ep", "2025-01-01", 89),
        new(2026, 1, "Nachtschatten", "2026-01-01", 89),
        new(2026, 5, "Sashimi Spezial", "2026-03-01", 89),
        new(2026, 9, "Könige der Nacht", "2026-05-03", 88)
    ];

    private static HttpResponseMessage LoginResponse()
    {
        var json = JsonSerializer.Serialize(new { data = new { token = "test-token" } }, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage EpisodesResponse(TvdbEpisode[] episodes)
    {
        var body = new
        {
            data = new { episodes },
            links = new { next = (string?)null }
        };
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var handler = new MockHandler(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains("login"))
            {
                return LoginResponse();
            }

            return EpisodesResponse(_allEpisodes);
        });

        services.AddSingleton<IOptionsMonitor<TvdbOptions>>(
            new TestOptionsMonitor<TvdbOptions>(new TvdbOptions { ApiKey = "test-key" }));
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<HttpMessageHandler>(handler);
        services.AddHttpClient<TvdbClient>()
            .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpMessageHandler>())
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api4.thetvdb.com/v4/"));
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
        {
            var actor = system.ActorOf(resolver.Props<TvdbEnrichmentActor>(), "tvdb-enrichment");
            registry.Register<TvdbEnrichmentActor>(actor);
        });
    }

    private IActorRef Actor => ActorRegistry.Get<TvdbEnrichmentActor>();

    [Fact]
    public void Season_filter_matches_in_season_first()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null)
        };

        Actor.Tell(new EnrichEpisodes(83214, 2026, candidates));

        var response = ExpectMsg<EnrichEpisodesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Episodes);
        Assert.Equal("2026", response.Episodes[0].Season);
        Assert.Equal("1", response.Episodes[0].Episode);
        Assert.True(response.Episodes[0].Confidence > 0.9f);
    }

    [Fact]
    public void Season_fallback_applies_penalty()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Old Season Ep", null, null, 5340, null, null)
        };

        Actor.Tell(new EnrichEpisodes(83214, 2026, candidates));

        var response = ExpectMsg<EnrichEpisodesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Episodes);
        Assert.Equal("2025", response.Episodes[0].Season);
        Assert.True(response.Episodes[0].Confidence <= 0.9f);
    }

    [Fact]
    public void Null_season_returns_all_episodes()
    {
        var candidates = new[]
        {
            new EpisodeCandidate(0, "Nachtschatten", null, null, 5340, null, null),
            new EpisodeCandidate(1, "Old Season Ep", null, null, 5340, null, null)
        };

        Actor.Tell(new EnrichEpisodes(83214, null, candidates));

        var response = ExpectMsg<EnrichEpisodesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, response.Episodes.Length);
    }

    private sealed class MockHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
