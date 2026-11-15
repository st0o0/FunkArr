using System.Net;
using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core.Configuration;
using FunkArr.Enrichment.Tmdb;
using FunkArr.Messages.Enrichment;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunkArr.Enrichment.Tests.Tmdb;

public sealed class TmdbEnrichmentActorTests : Akka.Hosting.TestKit.TestKit
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static HttpResponseMessage JsonResponse(object body)
    {
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
            var uri = request.RequestUri!.ToString();

            if (uri.Contains("find/"))
            {
                return JsonResponse(new
                {
                    movie_results = new[]
                    {
                        new { id = 550, title = "Fight Club", original_title = "Fight Club", release_date = "1999-10-15", runtime = 139, imdb_id = "tt0137523" }
                    }
                });
            }

            if (uri.Contains("alternative_titles"))
            {
                return JsonResponse(new { titles = new[] { new { title = "Clube da Luta" } } });
            }

            if (uri.Contains("movie/"))
            {
                return JsonResponse(new { id = 550, title = "Fight Club", original_title = "Fight Club", release_date = "1999-10-15", runtime = 139, imdb_id = "tt0137523" });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        services.AddSingleton<IOptionsMonitor<TmdbOptions>>(
            new TestOptionsMonitor<TmdbOptions>(new TmdbOptions { ApiKey = "test-key" }));
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<HttpMessageHandler>(handler);
        services.AddHttpClient<TmdbClient>()
            .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpMessageHandler>())
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.themoviedb.org/3/"));
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
        {
            var actor = system.ActorOf(resolver.Props<TmdbEnrichmentActor>(), "tmdb-enrichment");
            registry.Register<TmdbEnrichmentActor>(actor);
        });
    }

    private IActorRef Actor => ActorRegistry.Get<TmdbEnrichmentActor>();

    [Fact]
    public void TmdbId_takes_precedence_over_ImdbId()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        Actor.Tell(new EnrichMovies("tt0137523", 550, candidates));

        var response = ExpectMsg<EnrichMoviesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Movies);
        Assert.Equal(550, response.Movies[0].TmdbId);
    }

    [Fact]
    public void ImdbId_used_when_TmdbId_null()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        Actor.Tell(new EnrichMovies("tt0137523", null, candidates));

        var response = ExpectMsg<EnrichMoviesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Movies);
        Assert.Equal("Fight Club", response.Movies[0].Title);
    }

    [Fact]
    public void Neither_id_returns_failed()
    {
        var candidates = new[] { new MovieCandidate(0, "Fight Club", null, 8340) };

        Actor.Tell(new EnrichMovies(null, null, candidates));

        var response = ExpectMsg<EnrichMoviesFailed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.IsType<MovieNotFoundException>(response.Cause);
    }

    [Fact]
    public void Alt_titles_included_in_matching()
    {
        var candidates = new[] { new MovieCandidate(0, "Clube da Luta", null, 8340) };

        Actor.Tell(new EnrichMovies(null, 550, candidates));

        var response = ExpectMsg<EnrichMoviesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(response.Movies);
        Assert.Equal("Fight Club", response.Movies[0].Title);
    }

    private sealed class MockHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
