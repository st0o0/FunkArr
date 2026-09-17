using Akka.Actor;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class TvSearchWorkerStateTests
{
    private static readonly IActorRef _noSender = ActorRefs.Nobody;

    [Fact]
    public void Init_sets_all_fields_from_command()
    {
        var state = new TvSearchWorkerState();
        var id = Guid.NewGuid();
        var cmd = new SearchSeries(id, "sonarr", "Tatort", 2, 5, 83214, "tt123", 25, 10);

        state.Init(cmd, _noSender);

        Assert.Equal(id, state.SearchId);
        Assert.Equal("sonarr", state.Source);
        Assert.Equal("Tatort", state.Query);
        Assert.Equal(2, state.Season);
        Assert.Equal(83214, state.TvdbId);
        Assert.Equal("tt123", state.ImdbId);
        Assert.Equal(25, state.Limit);
        Assert.Equal(10, state.Offset);
    }

    [Fact]
    public void Apply_QueryMediathekCompleted_projects_to_SourceInfo()
    {
        var state = InitState();
        var items = new[]
        {
            MakeMediathekItem("ARD", "Tatort", "Ep1"),
            MakeMediathekItem("ZDF", "Show", "Ep2"),
        };

        state.Apply(new QueryMediathekCompleted(items, 2));

        Assert.Equal(2, state.Sources.Length);
        Assert.Equal("ARD", state.Sources[0].Channel);
        Assert.Equal("ZDF", state.Sources[1].Channel);
    }

    [Fact]
    public void Apply_ScoreCompleted_creates_EnrichedItems_with_null_Match()
    {
        var state = InitState(tvdbId: 83214);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));

        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.9, true, new MetadataSpec("1", "3", null))]));

        Assert.Single(state.Items);
        Assert.Equal(0.9, state.Items[0].Score);
        Assert.True(state.Items[0].Matched);
        Assert.True(state.Items[0].HasScoringMetadata);
        Assert.Equal("1", state.Items[0].Identity.Season);
        Assert.Equal("3", state.Items[0].Identity.Episode);
        Assert.Equal(83214, state.Items[0].Identity.TvdbId);
        Assert.Null(state.Items[0].Match);
    }

    [Fact]
    public void Apply_EnrichEpisodesCompleted_patches_identity_and_sets_match()
    {
        var state = InitState(tvdbId: 83214);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty,
            [new ScoredItem(0, 0.9, true, new MetadataSpec(null, null, null))]));

        state.Apply(new EnrichEpisodesCompleted(
            [new EnrichedEpisode(0, "2", "9", "Roomservice", 0.85f, MatchMethod.TitleMatch)]));

        Assert.Equal("2", state.Items[0].Identity.Season);
        Assert.Equal("9", state.Items[0].Identity.Episode);
        Assert.NotNull(state.Items[0].Match);
        Assert.Equal(0.85f, state.Items[0].Match.Confidence);
        Assert.Equal(MatchMethod.TitleMatch, state.Items[0].Match.Method);
    }

    [Fact]
    public void Apply_EnrichEpisodesCompleted_leaves_unmatched_items_unchanged()
    {
        var state = InitState(tvdbId: 83214);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem(), MakeMediathekItem()], 2));
        state.Apply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.9, true, new MetadataSpec(null, null, null)),
            new ScoredItem(1, 0.5, false),
        ]));

        state.Apply(new EnrichEpisodesCompleted(
            [new EnrichedEpisode(0, "1", "1", "Name", 0.9f, MatchMethod.TitleMatch)]));

        Assert.NotNull(state.Items[0].Match);
        Assert.Null(state.Items[1].Match);
        Assert.Null(state.Items[1].Identity.Season);
    }

    [Fact]
    public void TryGetRuleSetRequest_returns_true_when_sources_exist_and_no_ruleset()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem("ARD", "Tatort")], 1));

        var result = state.TryGetRuleSetRequest(out var request);

        Assert.True(result);
        Assert.NotNull(request);
        Assert.Equal("Tatort", request.TopicOrAlias);
    }

    [Fact]
    public void TryGetRuleSetRequest_returns_false_when_ruleset_already_set()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.ApplyRuleSet("tatort", "Tatort");

        var result = state.TryGetRuleSetRequest(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetRuleSetRequest_returns_true_with_id_lookup_when_no_sources()
    {
        var state = InitState(tvdbId: 83214, imdbId: "tt123");

        var result = state.TryGetRuleSetRequest(out var request);

        Assert.True(result);
        Assert.NotNull(request);
        Assert.Null(request.TopicOrAlias);
        Assert.Equal(83214, request.TvdbId);
        Assert.Equal("tt123", request.ImdbId);
    }

    [Fact]
    public void TryGetScoringRequest_returns_true_when_sources_and_ruleset_exist()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem("ARD", "Tatort", "Ep1")], 1));
        state.ApplyRuleSet("tatort", null);

        var result = state.TryGetScoringRequest(out var request);

        Assert.True(result);
        Assert.NotNull(request);
        Assert.Equal("tatort", request.RuleSetId);
        Assert.Single(request.Candidates);
        Assert.Equal("Ep1", request.Candidates[0].Title);
    }

    [Fact]
    public void TryGetScoringRequest_returns_false_when_sources_empty()
    {
        var state = InitState();
        state.ApplyRuleSet("tatort", null);

        var result = state.TryGetScoringRequest(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetScoringRequest_returns_false_when_no_ruleset()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));

        var result = state.TryGetScoringRequest(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetEnrichmentRequest_returns_true_when_matched_items_lack_season_episode()
    {
        var state = InitState(tvdbId: 83214);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty,
            [new ScoredItem(0, 0.9, true, new MetadataSpec(null, null, DateTimeOffset.UtcNow))]));

        var result = state.TryGetEnrichmentRequest(out var request);

        Assert.True(result);
        Assert.NotNull(request);
        Assert.Equal(83214, request.TvdbId);
        Assert.Single(request.Candidates);
    }

    [Fact]
    public void TryGetEnrichmentRequest_returns_false_when_no_tvdbId()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty,
            [new ScoredItem(0, 0.9, true, new MetadataSpec(null, null, null))]));

        var result = state.TryGetEnrichmentRequest(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetEnrichmentRequest_returns_false_when_all_items_have_season_episode()
    {
        var state = InitState(tvdbId: 83214);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty,
            [new ScoredItem(0, 0.9, true, new MetadataSpec("1", "3", null))]));

        var result = state.TryGetEnrichmentRequest(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetEnrichmentRequest_returns_false_when_metadata_null()
    {
        var state = InitState(tvdbId: 83214);
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem()], 1));
        state.Apply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.9, true)]));

        var result = state.TryGetEnrichmentRequest(out _);

        Assert.False(result);
    }

    [Fact]
    public void ToSearchCompleted_produces_sorted_variants()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted(
        [
            MakeMediathekItem("ARD", "Show", "Low", url: "https://low.mp4"),
            MakeMediathekItem("ARD", "Show", "High", url: "https://high.mp4"),
        ], 2));
        state.ApplyRuleSet("show", "Show");
        state.Apply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.5, true),
            new ScoredItem(1, 0.9, true),
        ]));

        var result = state.ToSearchCompleted();

        Assert.True(result.Items.Length >= 2);
        Assert.True(result.Items[0].Score >= result.Items[1].Score);
    }

    [Fact]
    public void ToSearchCompleted_returns_unscored_items_when_no_scoring()
    {
        var state = InitState();
        state.Apply(new QueryMediathekCompleted([MakeMediathekItem(url: "https://v.mp4")], 1));

        var result = state.ToSearchCompleted();

        Assert.Single(result.Items);
        Assert.Equal(0.0, result.Items[0].Score);
    }

    [Fact]
    public void ToSearchCompleted_returns_empty_when_no_sources()
    {
        var state = InitState();

        var result = state.ToSearchCompleted();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    private static TvSearchWorkerState InitState(
        int? tvdbId = null, string? imdbId = null, string? query = null)
    {
        var state = new TvSearchWorkerState();
        state.Init(new SearchSeries(Guid.NewGuid(), "sonarr", query, null, null, tvdbId, imdbId, null, null), _noSender);
        return state;
    }

    private static MediathekItem MakeMediathekItem(
        string channel = "ARD", string topic = "Tatort", string title = "Test",
        long timestamp = 0, string? url = "https://v.mp4") =>
        new(channel, topic, title, null, timestamp, 5400, 0, null, url, null, null, null);
}
