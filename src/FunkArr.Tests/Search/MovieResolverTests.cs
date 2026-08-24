using System.Text.Json;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.Search;
using FunkArr.Search.Resolvers;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Search;

public sealed class MovieResolverTests : Akka.Hosting.TestKit.TestKit
{
    private int _httpCallCount;

    private readonly Func<HttpRequestMessage, HttpResponseMessage> _defaultResponder;

    public MovieResolverTests()
    {
        _defaultResponder = req =>
        {
            Interlocked.Increment(ref _httpCallCount);
            var path = req.RequestUri!.AbsolutePath;

            if (path.Contains("/find/"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(new
                {
                    movie_results = new[]
                    {
                        new { id = 100, title = "Test Movie", original_title = "Test Movie", release_date = "2024-01-01" }
                    }
                }));
            }

            if (path.Contains("/search/movie"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(new
                {
                    results = new[]
                    {
                        new { id = 123, title = "Test Movie", original_title = "Test Movie", release_date = "2024-06-15" }
                    }
                }));
            }

            if (path.Contains("/movie/"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(new
                {
                    title = "Test Movie",
                    original_title = "Test Movie",
                    runtime = 120
                }));
            }

            return FakeHttpMessageHandler.JsonResponse("{}");
        };
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton(Options.Create(new SearchOptions { TmdbApiKey = "test-key" }));
        services.AddSingleton<TmdbClient>(sp =>
        {
            var httpClient = new HttpClient(new FakeHttpMessageHandler(_defaultResponder))
            {
                BaseAddress = new Uri("https://api.themoviedb.org/3/")
            };
            return new TmdbClient(httpClient, sp.GetRequiredService<IOptions<SearchOptions>>());
        });
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.AddHocon(
            """
            akka.persistence.journal.plugin = "akka.persistence.journal.inmem"
            akka.persistence.snapshot-store.plugin = "akka.persistence.snapshot-store.inmem"
            """,
            HoconAddMode.Prepend);
    }

    private IActorRef CreateActor(string? name = null)
    {
        var resolver = DependencyResolver.For(Sys);
        var props = resolver.Props<MovieResolver>();
        return Sys.ActorOf(props, name ?? $"movie-resolver-{Guid.NewGuid():N}");
    }

    [Fact(Timeout = 5000)]
    public async Task FindByImdbId_ReturnsMovieResolved()
    {
        var actor = CreateActor();
        var response = await actor.Ask<MovieResolved>(new ResolveMovie("tt1234567", null), TimeSpan.FromSeconds(3));

        Assert.NotNull(response.Info);
        Assert.Equal("Test Movie", response.Info.Title);
        Assert.Equal(120, response.Info.RuntimeMinutes);
    }

    [Fact(Timeout = 5000)]
    public async Task SearchFallback_ReturnsMovieResolved()
    {
        var actor = CreateActor();
        var response = await actor.Ask<MovieResolved>(new ResolveMovie(null, "Test Movie"), TimeSpan.FromSeconds(3));

        Assert.NotNull(response.Info);
        Assert.Equal("Test Movie", response.Info.Title);
        Assert.Equal(120, response.Info.RuntimeMinutes);
    }

    [Fact(Timeout = 10000)]
    public async Task CacheHit_SecondRequestSkipsHttp()
    {
        var actor = CreateActor();

        var first = await actor.Ask<MovieResolved>(new ResolveMovie("tt9999999", null), TimeSpan.FromSeconds(3));
        Assert.NotNull(first.Info);

        var callsAfterFirst = _httpCallCount;

        var second = await actor.Ask<MovieResolved>(new ResolveMovie("tt9999999", null), TimeSpan.FromSeconds(3));
        Assert.NotNull(second.Info);
        Assert.Equal("Test Movie", second.Info.Title);
        Assert.Equal(callsAfterFirst, _httpCallCount);
    }

    [Fact(Timeout = 10000)]
    public async Task RequestCoalescing_ConcurrentRequestsMakeSingleHttpCall()
    {
        var callCount = 0;
        var gate = new TaskCompletionSource();

        var handler = new FakeHttpMessageHandler(req =>
        {
            Interlocked.Increment(ref callCount);
            var path = req.RequestUri!.AbsolutePath;

            if (path.Contains("/find/"))
            {
                gate.Task.Wait();
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(new
                {
                    movie_results = new[]
                    {
                        new { id = 200, title = "Coalesced Movie", original_title = "Coalesced Movie", release_date = "2024-01-01" }
                    }
                }));
            }

            if (path.Contains("/movie/"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(new
                {
                    runtime = 90
                }));
            }

            return FakeHttpMessageHandler.JsonResponse("{}");
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.themoviedb.org/3/") };
        var tmdbClient = new TmdbClient(httpClient, Options.Create(new SearchOptions { TmdbApiKey = "test-key" }));
        var props = Props.Create(() => new MovieResolver(tmdbClient));
        var actor = Sys.ActorOf(props, "coalesce-test");

        var task1 = actor.Ask<MovieResolved>(new ResolveMovie("tt5555555", null), TimeSpan.FromSeconds(5));
        await Task.Delay(50);
        var task2 = actor.Ask<MovieResolved>(new ResolveMovie("tt5555555", null), TimeSpan.FromSeconds(5));

        gate.SetResult();

        var results = await Task.WhenAll(task1, task2);

        Assert.NotNull(results[0].Info);
        Assert.NotNull(results[1].Info);
        Assert.Equal("Coalesced Movie", results[0].Info!.Title);
        Assert.Equal("Coalesced Movie", results[1].Info!.Title);

        var findCalls = callCount;
        Assert.True(findCalls <= 2, $"Expected at most 2 HTTP calls (find + detail), got {findCalls}");
    }
}
