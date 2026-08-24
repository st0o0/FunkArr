using System.Text.Json;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using FunkArr.Search;
using FunkArr.Search.Resolvers;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.Search;

public sealed class SeriesResolverTests : Akka.Hosting.TestKit.TestKit
{
    private int _httpCallCount;
    private Func<HttpRequestMessage, HttpResponseMessage> _responder = DefaultResponder;

    private static HttpResponseMessage DefaultResponder(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;

        if (path.Contains("/episodes/"))
        {
            var episodes = new { data = new[] { new { airedSeason = 1, airedEpisodeNumber = 1, episodeName = "Pilot", firstAired = "2020-01-01", overview = "" } } };
            return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(episodes));
        }

        var show = new { data = new { seriesName = "Tatort", aliases = Array.Empty<string>(), overview = "" } };
        return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(show));
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<TvdbClient>(sp =>
        {
            var handler = new FakeHttpMessageHandler(req =>
            {
                Interlocked.Increment(ref _httpCallCount);
                return _responder(req);
            });
            return new TvdbClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.thetvdb.com/") });
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
        var props = resolver.Props<SeriesResolver>();
        return Sys.ActorOf(props, name ?? $"series-resolver-{Guid.NewGuid():N}");
    }

    [Fact(Timeout = 10000)]
    public async Task ResolveTvShow_ReturnsShowNameAndEpisodes()
    {
        var actor = CreateActor();

        var result = await actor.Ask<TvShowResolved>(
            new ResolveTvShow(12345, Season: 1), TimeSpan.FromSeconds(5));

        Assert.Equal("Tatort", result.ShowName);
        Assert.NotNull(result.Episodes);
        Assert.Single(result.Episodes);
        Assert.Equal("Pilot", result.Episodes[0].EpisodeName);
        Assert.Equal(1, result.Episodes[0].AiredSeason);
        Assert.Equal(1, result.Episodes[0].AiredEpisodeNumber);
    }

    [Fact(Timeout = 10000)]
    public async Task ResolveTvShow_CacheHit_DoesNotMakeSecondShowLookup()
    {
        var actor = CreateActor();

        await actor.Ask<TvShowResolved>(
            new ResolveTvShow(77777, Season: null), TimeSpan.FromSeconds(5));

        var callsBefore = _httpCallCount;

        var result = await actor.Ask<TvShowResolved>(
            new ResolveTvShow(77777, Season: null), TimeSpan.FromSeconds(5));

        Assert.Equal("Tatort", result.ShowName);
        Assert.Equal(callsBefore, _httpCallCount);
    }

    [Fact(Timeout = 10000)]
    public async Task ResolveTvShow_ConcurrentSameId_OnlyOneHttpCall()
    {
        var tcs = new TaskCompletionSource<HttpResponseMessage>();
        _responder = req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("/episodes/"))
            {
                var episodes = new { data = new[] { new { airedSeason = 1, airedEpisodeNumber = 1, episodeName = "Pilot", firstAired = "2020-01-01", overview = "" } } };
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(episodes));
            }

            return tcs.Task.GetAwaiter().GetResult();
        };

        var actor = CreateActor();

        var task1 = actor.Ask<TvShowResolved>(new ResolveTvShow(55555, Season: null), TimeSpan.FromSeconds(5));
        var task2 = actor.Ask<TvShowResolved>(new ResolveTvShow(55555, Season: null), TimeSpan.FromSeconds(5));

        await Task.Delay(100);

        var show = new { data = new { seriesName = "Tatort", aliases = Array.Empty<string>(), overview = "" } };
        tcs.SetResult(FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(show)));

        var results = await Task.WhenAll(task1, task2);

        Assert.Equal("Tatort", results[0].ShowName);
        Assert.Equal("Tatort", results[1].ShowName);
        // One show lookup call (no episode calls since Season is null)
        Assert.Equal(1, _httpCallCount);
    }

    [Fact(Timeout = 10000)]
    public async Task RecoveryCompleted_ActorStartsSuccessfully()
    {
        var actor = CreateActor();

        // Verify the actor is alive and responsive after recovery
        var result = await actor.Ask<TvShowResolved>(
            new ResolveTvShow(99999, Season: null), TimeSpan.FromSeconds(5));

        Assert.Equal("Tatort", result.ShowName);
    }
}
