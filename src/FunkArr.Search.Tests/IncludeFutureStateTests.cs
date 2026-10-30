using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class IncludeFutureStateTests
{
    private static readonly IActorRef _noSender = ActorRefs.Nobody;

    [Fact]
    public void SearchOptions_default_includes_future()
    {
        Assert.True(new SearchOptions().IncludeFuture);
    }

    [Fact]
    public void Tv_include_future_adds_identical_query_with_future_enabled()
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString(), includeFuture: true);
        state.Init(new SearchSeries(id, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), _noSender);

        var queries = state.GetMediathekQueries();

        Assert.Equal(2, queries.Length);
        Assert.Multiple(
            () => Assert.False(queries[0].Future),
            () => Assert.True(queries[1].Future),
            () => Assert.Equal(queries[0] with { Future = true }, queries[1]));
    }

    [Fact]
    public void Movie_include_future_adds_identical_query_with_future_enabled()
    {
        var id = Guid.NewGuid();
        var state = MovieSearchWorkerState.From(id.ToString(), includeFuture: true);
        state.Apply(new SearchMovie(id, SearchSource.Radarr, "Das Boot", null, null, null, null), _noSender);

        var queries = state.GetMediathekQueries();

        Assert.Equal(2, queries.Length);
        Assert.Multiple(
            () => Assert.False(queries[0].Future),
            () => Assert.True(queries[1].Future),
            () => Assert.Equal(queries[0] with { Future = true }, queries[1]));
    }

    [Fact]
    public void Excluding_future_sends_a_single_past_query()
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString(), includeFuture: false);
        state.Init(new SearchSeries(id, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), _noSender);

        var query = Assert.Single(state.GetMediathekQueries());

        Assert.False(query.Future);
    }

    [Fact]
    public void Default_state_includes_future()
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString());
        state.Init(new SearchSeries(id, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), _noSender);

        Assert.Contains(state.GetMediathekQueries(), q => q.Future);
    }
}
