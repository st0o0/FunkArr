using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class PreReleasedQueryStateTests
{
    private static readonly IActorRef _noSender = ActorRefs.Nobody;

    [Fact]
    public void Tv_search_adds_identical_query_with_future_enabled()
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString());
        state.Init(new SearchSeries(id, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), _noSender);

        var queries = state.GetMediathekQueries();

        Assert.Equal(2, queries.Length);
        Assert.Multiple(
            () => Assert.False(queries[0].Future),
            () => Assert.True(queries[1].Future),
            () => Assert.Equal(queries[0] with { Future = true }, queries[1]));
    }

    [Fact]
    public void Movie_search_adds_identical_query_with_future_enabled()
    {
        var id = Guid.NewGuid();
        var state = MovieSearchWorkerState.From(id.ToString());
        state.Apply(new SearchMovie(id, SearchSource.Radarr, "Das Boot", null, null, null, null), _noSender);

        var queries = state.GetMediathekQueries();

        Assert.Equal(2, queries.Length);
        Assert.Multiple(
            () => Assert.False(queries[0].Future),
            () => Assert.True(queries[1].Future),
            () => Assert.Equal(queries[0] with { Future = true }, queries[1]));
    }
}
