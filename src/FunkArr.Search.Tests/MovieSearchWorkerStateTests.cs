using Akka.Actor;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class MovieSearchWorkerStateTests
{
    private static readonly IActorRef NoSender = ActorRefs.Nobody;

    [Fact]
    public void Init_sets_all_fields_from_command()
    {
        var state = new MovieSearchWorkerState();
        var id = Guid.NewGuid();
        var cmd = new SearchMovie(id, "radarr", "Das Boot", "tt0806910", 550, 25, 10);

        state.Init(cmd, NoSender);

        Assert.Equal(id, state.SearchId);
        Assert.Equal("radarr", state.Source);
        Assert.Equal("Das Boot", state.Query);
        Assert.Equal("tt0806910", state.ImdbId);
        Assert.Equal(550, state.TmdbId);
        Assert.Equal(25, state.Limit);
        Assert.Equal(10, state.Offset);
    }

    [Fact]
    public void Apply_QueryMediathekCompleted_projects_to_SourceInfo()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem("ZDF", "Film", "Das Boot")], 1));

        Assert.Single(state.Sources);
        Assert.Equal("ZDF", state.Sources[0].Channel);
        Assert.Equal("Film", state.Sources[0].Topic);
    }

    [Fact]
    public void Apply_ScoreCompleted_creates_EnrichedItems_with_BaseIdentity()
    {
        var state = InitState(imdbId: "tt0806910", tmdbId: 550);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));

        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        Assert.Single(state.Items);
        Assert.Equal(0.8, state.Items[0].Score);
        Assert.Equal("tt0806910", state.Items[0].Identity.ImdbId);
        Assert.Equal(550, state.Items[0].Identity.TmdbId);
        Assert.Null(state.Items[0].Identity.TvdbId);
        Assert.Null(state.Items[0].Match);
    }

    [Fact]
    public void Apply_EnrichMoviesCompleted_patches_identity_and_sets_match()
    {
        var state = InitState(imdbId: "tt0806910", tmdbId: 550);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        state.Apply(new EnrichMoviesCompleted(
            [new EnrichedMovie(0, "Das Boot", 1981, "tt0806910", 550, 0.92f, MatchMethod.TitleMatch)]));

        Assert.Equal("tt0806910", state.Items[0].Identity.ImdbId);
        Assert.Equal(550, state.Items[0].Identity.TmdbId);
        Assert.NotNull(state.Items[0].Match);
        Assert.Equal(0.92f, state.Items[0].Match.Confidence);
        Assert.Equal(MatchMethod.TitleMatch, state.Items[0].Match.Method);
    }

    [Fact]
    public void Apply_EnrichMoviesCompleted_overwrites_ids_from_enrichment()
    {
        var state = InitState(imdbId: null, tmdbId: 550);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        state.Apply(new EnrichMoviesCompleted(
            [new EnrichedMovie(0, "Film", 2024, "tt999", 550, 0.9f, MatchMethod.TitleMatch)]));

        Assert.Equal("tt999", state.Items[0].Identity.ImdbId);
    }

    [Fact]
    public void TryGetRuleSetRequest_returns_true_with_id_lookup()
    {
        var state = InitState(imdbId: "tt123", tmdbId: 550);

        var result = state.TryGetRuleSetRequest(out var request);

        Assert.True(result);
        Assert.NotNull(request);
        Assert.Null(request.TopicOrAlias);
        Assert.Equal("tt123", request.ImdbId);
    }

    [Fact]
    public void TryGetRuleSetRequest_returns_false_when_ruleset_already_set()
    {
        var state = InitState(imdbId: "tt123");
        state.ApplyRuleSet("film", null);

        Assert.False(state.TryGetRuleSetRequest(out _));
    }

    [Fact]
    public void TryGetEnrichmentRequest_returns_true_when_matched_items_exist()
    {
        var state = InitState(imdbId: "tt123", tmdbId: 550);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        var result = state.TryGetEnrichmentRequest(out var request);

        Assert.True(result);
        Assert.NotNull(request);
        Assert.Equal("tt123", request.ImdbId);
        Assert.Equal(550, request.TmdbId);
        Assert.Single(request.Candidates);
    }

    [Fact]
    public void TryGetEnrichmentRequest_returns_false_when_no_movie_ids()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        Assert.False(state.TryGetEnrichmentRequest(out _));
    }

    [Fact]
    public void TryGetEnrichmentRequest_returns_false_when_no_matched_items()
    {
        var state = InitState(imdbId: "tt123");
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.0, false)]));

        Assert.False(state.TryGetEnrichmentRequest(out _));
    }

    [Fact]
    public void TryGetScoringRequest_returns_true_when_ready()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem("ZDF", "Film", "Movie")], 1));
        state.ApplyRuleSet("film", null);

        var result = state.TryGetScoringRequest(out var request);

        Assert.True(result);
        Assert.NotNull(request);
        Assert.Equal("film", request.RuleSetId);
    }

    [Fact]
    public void TryGetScoringRequest_returns_false_when_sources_empty()
    {
        var state = InitState();
        state.ApplyRuleSet("film", null);

        Assert.False(state.TryGetScoringRequest(out _));
    }

    [Fact]
    public void TryGetMediathekQuery_uses_title_and_topic_fields_for_movies()
    {
        var state = InitState(query: "Das Boot");

        var result = state.TryGetMediathekQuery(out var query);

        Assert.True(result);
        Assert.NotNull(query);
        Assert.Equal(3600, query.DurationMin);
        Assert.Contains("title", query.Fields[0].Fields);
        Assert.Contains("topic", query.Fields[0].Fields);
    }

    [Fact]
    public void ToSearchCompleted_returns_movie_completed_type()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem(url: "https://v.mp4")], 1));

        var result = state.ToSearchCompleted();

        Assert.IsType<SearchMovieCompleted>(result);
        Assert.Single(result.Items);
    }

    private static MovieSearchWorkerState InitState(
        string? imdbId = null, int? tmdbId = null, string? query = null)
    {
        var state = new MovieSearchWorkerState();
        state.Init(new SearchMovie(Guid.NewGuid(), "radarr", query, imdbId, tmdbId, null, null), NoSender);
        return state;
    }

    private static MediathekItem MakeMediathekItem(
        string channel = "ARD", string topic = "Film", string title = "Test",
        string? url = "https://v.mp4") =>
        new(channel, topic, title, null, 0, 5400, 0, null, url, null, null, null);
}
