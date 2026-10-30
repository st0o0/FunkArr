using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class SearchWorkerStateTests
{
    private static readonly IActorRef _noSender = ActorRefs.Nobody;

    [Fact]
    public void Tv_init_sets_all_fields_from_command()
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString());

        state.Init(new SearchSeries(id, SearchSource.Sonarr, "Tatort", 2, 5, 83214, "tt123", 25, 10), _noSender);

        Assert.Multiple(
            () => Assert.Equal(id, state.SearchId),
            () => Assert.Equal(SearchSource.Sonarr, state.Source),
            () => Assert.Equal("Tatort", state.Query),
            () => Assert.Equal(2, state.Season),
            () => Assert.Equal(83214, state.TvdbId),
            () => Assert.Equal("tt123", state.ImdbId),
            () => Assert.Equal(25, state.Limit),
            () => Assert.Equal(10, state.Offset));
    }

    [Fact]
    public void Tv_ruleset_request_uses_ids_only()
    {
        var state = TvState(query: "Tatort", tvdbId: 83214, imdbId: "tt123");

        Assert.True(state.TryGetRuleSetRequest(out var request));
        Assert.NotNull(request);
        Assert.Multiple(
            () => Assert.Null(request.TopicOrAlias),
            () => Assert.Equal(83214, request.Ids?.TvdbId),
            () => Assert.Equal("tt123", request.Ids?.ImdbId));
    }

    [Fact]
    public void Tv_text_search_needs_no_ruleset_request()
    {
        var state = TvState(query: "Tatort");

        Assert.False(state.TryGetRuleSetRequest(out _));
    }

    [Fact]
    public void Tv_queries_use_query_offset_and_limit_without_ruleset()
    {
        var state = TvState(query: "Tatort", limit: 25, offset: 10);

        var query = state.GetMediathekQuery();
        var field = Assert.Single(query.Fields);
        Assert.Multiple(
            () => Assert.Equal("topic", Assert.Single(field.Fields)),
            () => Assert.Equal("Tatort", field.Query),
            () => Assert.Equal(10, query.Offset),
            () => Assert.Equal(25, query.Size),
            () => Assert.Equal(300, query.DurationMin));
    }

    [Fact]
    public void Tv_queries_use_ruleset_topic_from_offset_zero()
    {
        var state = TvState(tvdbId: 83214, offset: 10);
        state.ApplyRuleSet(new RuleSetResolved("tatort", "Tatort"));

        var query = state.GetMediathekQuery();
        Assert.Multiple(
            () => Assert.Equal("Tatort", Assert.Single(query.Fields).Query),
            () => Assert.Equal(0, query.Offset));
    }

    [Fact]
    public void Tv_without_query_or_ruleset_queries_recent_items()
    {
        var state = TvState();

        var query = state.GetMediathekQuery();
        Assert.Empty(query.Fields);
    }

    [Fact]
    public void Tv_to_evaluation_carries_items_ids_and_ruleset()
    {
        var state = TvState(tvdbId: 83214, imdbId: "tt123", season: 2026);
        var ruleSet = new RuleSetResolved("tatort", "Tatort");
        state.ApplyRuleSet(ruleSet);
        var item = Item("Roomservice");
        state.Apply(new QueryMediathekCompleted([item], 1));
        var evaluationId = Guid.NewGuid();

        var evaluation = state.ToEvaluation(evaluationId);

        Assert.Multiple(
            () => Assert.Equal(evaluationId, evaluation.SearchId),
            () => Assert.Equal(SearchSource.Sonarr, evaluation.Source),
            () => Assert.Equal(item, Assert.Single(evaluation.Items)),
            () => Assert.Equal(83214, evaluation.TvdbId),
            () => Assert.Equal("tt123", evaluation.ImdbId),
            () => Assert.Equal(2026, evaluation.Season),
            () => Assert.Equal(ruleSet, evaluation.RuleSet));
    }

    [Fact]
    public void Tv_search_completed_uses_search_id_and_evaluated_items()
    {
        var state = TvState(query: "Tatort");
        var result = new SearchResultItem("Tatort.S01E01.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Tatort",
            "https://x.com/v.mp4", 5400, 0, 720, null, 0.9);

        var completed = state.ToSearchCompleted(new EvaluateSeriesCompleted(Guid.NewGuid(), [result], 1));

        Assert.Multiple(
            () => Assert.Equal(state.SearchId, completed.SearchId),
            () => Assert.Equal(result, Assert.Single(completed.Items)),
            () => Assert.Equal(1, completed.Total));
    }

    [Fact]
    public void Movie_queries_use_title_and_topic_fields()
    {
        var state = MovieState(query: "Das Boot");

        var query = state.GetMediathekQuery();
        var field = Assert.Single(query.Fields);
        Assert.Multiple(
            () => Assert.Equal(new[] { "title", "topic" }, field.Fields),
            () => Assert.Equal("Das Boot", field.Query),
            () => Assert.Equal(3600, query.DurationMin));
    }

    [Fact]
    public void Movie_ruleset_request_uses_ids_only()
    {
        var state = MovieState(query: "Das Boot", imdbId: "tt0082096", tmdbId: 387);

        Assert.True(state.TryGetRuleSetRequest(out var request));
        Assert.NotNull(request);
        Assert.Multiple(
            () => Assert.Null(request.TopicOrAlias),
            () => Assert.Equal("tt0082096", request.Ids?.ImdbId),
            () => Assert.Equal(387, request.Ids?.TmdbId));
    }

    [Fact]
    public void Movie_to_evaluation_carries_items_ids_and_ruleset()
    {
        var state = MovieState(imdbId: "tt0082096", tmdbId: 387);
        var ruleSet = new RuleSetResolved("das-boot", "Das Boot");
        state.ApplyRuleSet(ruleSet);
        state.Apply(new QueryMediathekCompleted([Item("Das Boot")], 1));

        var evaluation = state.ToEvaluation(Guid.NewGuid());

        Assert.Multiple(
            () => Assert.Equal(SearchSource.Radarr, evaluation.Source),
            () => Assert.Equal("Das Boot", Assert.Single(evaluation.Items).Title),
            () => Assert.Equal("tt0082096", evaluation.ImdbId),
            () => Assert.Equal(387, evaluation.TmdbId),
            () => Assert.Equal(ruleSet, evaluation.RuleSet));
    }

    [Fact]
    public void Movie_empty_returns_search_id_and_no_items()
    {
        var state = MovieState(query: "Das Boot");

        var empty = state.Empty();

        Assert.Multiple(
            () => Assert.Equal(state.SearchId, empty.SearchId),
            () => Assert.Empty(empty.Items),
            () => Assert.Equal(0, empty.Total));
    }

    private static TvSearchWorkerState TvState(string? query = null, int? tvdbId = null, string? imdbId = null,
        int? season = null, int? limit = null, int? offset = null)
    {
        var id = Guid.NewGuid();
        var state = TvSearchWorkerState.From(id.ToString());
        state.Init(new SearchSeries(id, SearchSource.Sonarr, query, season, null, tvdbId, imdbId, limit, offset), _noSender);
        return state;
    }

    private static MovieSearchWorkerState MovieState(string? query = null, string? imdbId = null, int? tmdbId = null)
    {
        var id = Guid.NewGuid();
        var state = MovieSearchWorkerState.From(id.ToString());
        state.Apply(new SearchMovie(id, SearchSource.Radarr, query, imdbId, tmdbId, null, null), _noSender);
        return state;
    }

    private static MediathekItem Item(string title) =>
        new("ARD", title, title, null, 1719244800, 5400, 0, null, "https://x.com/v.mp4", null, null, null);
}
