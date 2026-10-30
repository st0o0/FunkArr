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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Tv_queries_use_include_future(bool includeFuture)
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString(), includeFuture: includeFuture);
        state.Init(new SearchSeries(id, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), _noSender);

        Assert.True(state.TryGetMediathekQuery(out var query));
        Assert.True(state.TryGetMediathekQueryForTopic("Tatort", out var topicQuery));
        Assert.NotNull(query);
        Assert.NotNull(topicQuery);
        Assert.Multiple(
            () => Assert.Equal(includeFuture, query.Future),
            () => Assert.Equal(includeFuture, topicQuery.Future));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Movie_queries_use_include_future(bool includeFuture)
    {
        var id = Guid.NewGuid();
        var state = MovieSearchWorkerState.From(id.ToString(), includeFuture: includeFuture);
        state.Apply(new SearchMovie(id, SearchSource.Radarr, "Das Boot", null, null, null, null), _noSender);

        Assert.True(state.TryGetMediathekQuery(out var query));
        Assert.True(state.TryGetMediathekQueryForTopic("Das Boot", out var topicQuery));
        Assert.NotNull(query);
        Assert.NotNull(topicQuery);
        Assert.Multiple(
            () => Assert.Equal(includeFuture, query.Future),
            () => Assert.Equal(includeFuture, topicQuery.Future));
    }

    [Fact]
    public void Default_state_includes_future()
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString());
        state.Init(new SearchSeries(id, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), _noSender);

        Assert.True(state.TryGetMediathekQuery(out var query));
        Assert.NotNull(query);
        Assert.True(query.Future);
    }
}
