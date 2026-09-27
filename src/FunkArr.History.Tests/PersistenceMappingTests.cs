using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence;

namespace FunkArr.History.Tests;

public sealed class PersistenceMappingTests
{
    private static readonly DateTimeOffset _baseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(SearchSource.Sonarr, PersistedSearchSource.Sonarr)]
    [InlineData(SearchSource.Radarr, PersistedSearchSource.Radarr)]
    [InlineData(SearchSource.Prowlarr, PersistedSearchSource.Prowlarr)]
    [InlineData(SearchSource.Test, PersistedSearchSource.Test)]
    public void SearchSource_DirectRoundtrip_PreservesValue(SearchSource domain, PersistedSearchSource expected)
    {
        var persisted = domain.ToPersistence();
        Assert.Equal(expected, persisted);

        var restored = persisted.ToDomain();
        Assert.Equal(domain, restored);
    }

    [Fact]
    public void TracedIdentification_DirectRoundtrip()
    {
        var original = new TracedIdentification("2", "15", "Tatort: Sturm");

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal("2", restored.Season);
        Assert.Equal("15", restored.Episode);
        Assert.Equal("Tatort: Sturm", restored.Title);
    }

    [Fact]
    public void TracedIdentification_DirectRoundtrip_NullFields()
    {
        var original = new TracedIdentification(null, null, null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Null(restored.Season);
        Assert.Null(restored.Episode);
        Assert.Null(restored.Title);
    }

    [Fact]
    public void IdentificationTrace_DirectRoundtrip_WithStrategy()
    {
        var original = new IdentificationTrace(
            IdentificationStrategy.SeasonAndEpisodeNumber, true, null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.NotNull(restored.Strategy);
        Assert.Equal(IdentificationStrategy.SeasonAndEpisodeNumber, restored.Strategy.Value);
        Assert.True(restored.Attempted);
        Assert.Null(restored.Detail);
    }

    [Fact]
    public void IdentificationTrace_DirectRoundtrip_WithFailureReason()
    {
        var original = new IdentificationTrace(
            IdentificationStrategy.TitleExact, true,
            IdentificationFailureReason.TitleDoesNotMatch);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.NotNull(restored.Detail);
        Assert.Equal(IdentificationFailureReason.TitleDoesNotMatch, restored.Detail.Value);
    }

    [Fact]
    public void IdentificationTrace_DirectRoundtrip_NullStrategy()
    {
        var original = new IdentificationTrace(null, false, null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Null(restored.Strategy);
        Assert.False(restored.Attempted);
        Assert.Null(restored.Detail);
    }

    [Fact]
    public void EnrichmentTrace_DirectRoundtrip_AllFieldsPopulated()
    {
        var original = new EnrichmentTrace(
            MatchMethod.TitleMatch, 0.85f, true,
            "1", "5", "Resolved Title", 2026, 3, "Matched via title");

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(MatchMethod.TitleMatch, restored.Method);
        Assert.Equal(0.85f, restored.Confidence, 0.001f);
        Assert.True(restored.Enriched);
        Assert.Equal("1", restored.ResolvedSeason);
        Assert.Equal("5", restored.ResolvedEpisode);
        Assert.Equal("Resolved Title", restored.ResolvedTitle);
        Assert.Equal(2026, restored.ResolvedYear);
        Assert.Equal(3, restored.DaysDiff);
        Assert.Equal("Matched via title", restored.Detail);
    }

    [Fact]
    public void EnrichmentTrace_DirectRoundtrip_NullOptionalFields()
    {
        var original = new EnrichmentTrace(MatchMethod.AirdateMatch, 0.5f, false);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(MatchMethod.AirdateMatch, restored.Method);
        Assert.False(restored.Enriched);
        Assert.Null(restored.ResolvedSeason);
        Assert.Null(restored.ResolvedTitle);
    }

    [Fact]
    public void FilterNodeTrace_DirectRoundtrip()
    {
        var original = new FilterNodeTrace("title", "contains", "Tatort", "Tatort: Sturm", true, false, null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal("title", restored.Field);
        Assert.Equal("contains", restored.Op);
        Assert.Equal("Tatort", restored.ExpectedValue);
        Assert.Equal("Tatort: Sturm", restored.ActualValue);
        Assert.True(restored.Passed);
        Assert.False(restored.Skipped);
        Assert.Null(restored.Group);
    }

    [Fact]
    public void FilterGroupTrace_DirectRoundtrip_NestedGroup()
    {
        var innerGroup = new FilterGroupTrace(FilterGroupOp.Any, true,
            [new FilterNodeTrace("topic", "contains", "Krimi", "Krimi", true, false, null)]);
        var outerNode = new FilterNodeTrace(null, null, null, null, true, false, innerGroup);
        var original = new FilterGroupTrace(FilterGroupOp.All, true, [outerNode]);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        var node = Assert.Single(restored.Nodes);
        Assert.NotNull(node.Group);
        Assert.Equal(FilterGroupOp.Any, node.Group.Operator);
    }

    [Theory]
    [InlineData(RuleOutcome.Matched)]
    [InlineData(RuleOutcome.FilterFailed)]
    [InlineData(RuleOutcome.IdentificationFailed)]
    public void RuleTrace_DirectRoundtrip_PreservesOutcome(RuleOutcome outcome)
    {
        var original = new RuleTrace("rule-42", 5, outcome, null, null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal("rule-42", restored.RuleId);
        Assert.Equal(5, restored.Priority);
        Assert.Equal(outcome, restored.Outcome);
    }

    [Fact]
    public void ItemTrace_DirectRoundtrip_FullyPopulated()
    {
        var candidate = new ScoreCandidate("Tatort", "Krimi", "ARD", 5400, 720, "A crime story", 1719360000);
        var identification = new TracedIdentification("1", "5", "Tatort");
        var ruleTraces = new[]
        {
            new RuleTrace("rule-1", 0, RuleOutcome.Matched,
                new FilterGroupTrace(FilterGroupOp.All, true, []),
                new IdentificationTrace(IdentificationStrategy.SeasonAndEpisodeNumber, true, null)),
        };
        var enrichment = new EnrichmentTrace(MatchMethod.TitleMatch, 0.9f, true, "1", "5", "Tatort", 2026, 0, null);
        var original = new ItemTrace(candidate, true, 0.95, "rule-1", identification, ruleTraces, enrichment);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal("Tatort", restored.Candidate.Title);
        Assert.Equal(720, restored.Candidate.Quality);
        Assert.True(restored.Matched);
        Assert.Equal(0.95, restored.Score, 0.001);
        Assert.NotNull(restored.Identification);
        Assert.Equal("1", restored.Identification.Season);
        Assert.NotNull(restored.EnrichmentTrace);
        Assert.Equal(MatchMethod.TitleMatch, restored.EnrichmentTrace.Method);
    }

    [Fact]
    public void ItemTrace_DirectRoundtrip_NullOptionalFields()
    {
        var candidate = new ScoreCandidate("Test", "", "ZDF", 0, 0, null, 0);
        var original = new ItemTrace(candidate, false, 0.0, null, null, [], null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Null(restored.MatchedRuleId);
        Assert.Null(restored.Identification);
        Assert.Empty(restored.RuleTraces);
        Assert.Null(restored.EnrichmentTrace);
    }

    [Theory]
    [InlineData(MatchMethod.RegexExtracted)]
    [InlineData(MatchMethod.TitleMatch)]
    [InlineData(MatchMethod.AirdateMatch)]
    [InlineData(MatchMethod.YearMatch)]
    public void EnrichmentTrace_DirectRoundtrip_AllMatchMethods(MatchMethod method)
    {
        var original = new EnrichmentTrace(method, 0.7f, true);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(method, restored.Method);
    }

    [Theory]
    [InlineData(IdentificationStrategy.SeasonAndEpisodeNumber)]
    [InlineData(IdentificationStrategy.AbsoluteEpisodeNumber)]
    [InlineData(IdentificationStrategy.TitleExact)]
    [InlineData(IdentificationStrategy.TitleIncludes)]
    [InlineData(IdentificationStrategy.AirdateExtraction)]
    public void IdentificationTrace_DirectRoundtrip_AllStrategies(IdentificationStrategy strategy)
    {
        var original = new IdentificationTrace(strategy, true, null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.NotNull(restored.Strategy);
        Assert.Equal(strategy, restored.Strategy.Value);
    }

    [Theory]
    [InlineData(FilterGroupOp.All)]
    [InlineData(FilterGroupOp.Any)]
    [InlineData(FilterGroupOp.Not)]
    public void FilterGroupTrace_DirectRoundtrip_AllOperators(FilterGroupOp op)
    {
        var original = new FilterGroupTrace(op, true, []);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(op, restored.Operator);
    }

    [Theory]
    [InlineData(IdentificationFailureReason.UnknownStrategy)]
    [InlineData(IdentificationFailureReason.SeasonPatternNotMatched)]
    [InlineData(IdentificationFailureReason.NoEpisodePatternConfigured)]
    [InlineData(IdentificationFailureReason.EpisodePatternNotMatched)]
    [InlineData(IdentificationFailureReason.NoTitlePartsConfigured)]
    [InlineData(IdentificationFailureReason.TitlePartRegexNotMatched)]
    [InlineData(IdentificationFailureReason.TitleDoesNotMatch)]
    [InlineData(IdentificationFailureReason.NoDateFoundInTitle)]
    public void IdentificationTrace_DirectRoundtrip_AllFailureReasons(IdentificationFailureReason reason)
    {
        var original = new IdentificationTrace(IdentificationStrategy.TitleExact, true, reason);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.NotNull(restored.Detail);
        Assert.Equal(reason, restored.Detail.Value);
    }

    [Theory]
    [InlineData(SearchSource.Sonarr)]
    [InlineData(SearchSource.Radarr)]
    [InlineData(SearchSource.Prowlarr)]
    [InlineData(SearchSource.Test)]
    public void SearchSource_Roundtrip_AllValues(SearchSource source)
    {
        var origin = new ScoringOrigin(source, "test-query");
        var cmd = new RecordHistory(Guid.NewGuid(), "rs-1", origin, _baseTime, 5, 2, 1, []);

        var (state, _) = HistoryState.Empty.ProcessCommand(cmd);

        var persisted = state.GetPersistenceState();
        var restored = HistoryState.FromPersistence(persisted);

        var entry = Assert.Single(restored.Snapshots);
        Assert.Equal(source, entry.Origin.Source);
        Assert.Equal("test-query", entry.Origin.Query);
    }

    [Fact]
    public void ItemTrace_Roundtrip_WithAllFields()
    {
        var candidate = new ScoreCandidate("Title", "Topic", "ARD", 3600, 720, "Description", _baseTime.ToUnixTimeSeconds());
        var filterNode = new FilterNodeTrace("title", "contains", "test", "Test Title", true, false, null);
        var filterGroup = new FilterGroupTrace(FilterGroupOp.All, true, [filterNode]);
        var ruleTrace = new RuleTrace("rule-1", 0, RuleOutcome.Matched,
            filterGroup,
            new IdentificationTrace(IdentificationStrategy.TitleIncludes, true, null));
        var identification = new TracedIdentification("1", "5", "Episode Title");
        var enrichment = new EnrichmentTrace(MatchMethod.TitleMatch, 0.95f, true, "1", "5", "Episode", 2026, 0, null);

        var trace = new ItemTrace(candidate, true, 100, "rule-1", identification, [ruleTrace], enrichment);
        var cmd = new RecordHistory(Guid.NewGuid(), "rs-1",
            new ScoringOrigin(SearchSource.Sonarr, "query"), _baseTime, 1, 1, 1, [trace]);

        var (state, _) = HistoryState.Empty.ProcessCommand(cmd);
        var restored = HistoryState.FromPersistence(state.GetPersistenceState());

        var entry = Assert.Single(restored.Snapshots);
        var restoredTrace = Assert.Single(entry.ItemTraces);

        Assert.True(restoredTrace.Matched);
        Assert.Equal(100, restoredTrace.Score);
        Assert.Equal("rule-1", restoredTrace.MatchedRuleId);

        Assert.NotNull(restoredTrace.Candidate);
        Assert.Equal("Title", restoredTrace.Candidate.Title);
        Assert.Equal("Topic", restoredTrace.Candidate.Topic);
        Assert.Equal("ARD", restoredTrace.Candidate.Channel);
        Assert.Equal(3600, restoredTrace.Candidate.Duration);
        Assert.Equal(720, restoredTrace.Candidate.Quality);
        Assert.Equal("Description", restoredTrace.Candidate.Description);

        Assert.NotNull(restoredTrace.Identification);
        Assert.Equal("1", restoredTrace.Identification.Season);
        Assert.Equal("5", restoredTrace.Identification.Episode);
        Assert.Equal("Episode Title", restoredTrace.Identification.Title);

        var restoredRule = Assert.Single(restoredTrace.RuleTraces);
        Assert.Equal("rule-1", restoredRule.RuleId);
        Assert.Equal(0, restoredRule.Priority);
        Assert.Equal(RuleOutcome.Matched, restoredRule.Outcome);

        Assert.NotNull(restoredRule.FilterTrace);
        Assert.Equal(FilterGroupOp.All, restoredRule.FilterTrace.Operator);
        Assert.True(restoredRule.FilterTrace.Passed);

        Assert.NotNull(restoredRule.IdentificationTrace);
        Assert.NotNull(restoredRule.IdentificationTrace.Strategy);
        Assert.Equal(IdentificationStrategy.TitleIncludes, restoredRule.IdentificationTrace.Strategy.Value);
        Assert.True(restoredRule.IdentificationTrace.Attempted);

        Assert.NotNull(restoredTrace.EnrichmentTrace);
        Assert.Equal(MatchMethod.TitleMatch, restoredTrace.EnrichmentTrace.Method);
        Assert.Equal(0.95f, restoredTrace.EnrichmentTrace.Confidence);
        Assert.True(restoredTrace.EnrichmentTrace.Enriched);
        Assert.Equal("1", restoredTrace.EnrichmentTrace.ResolvedSeason);
        Assert.Equal("5", restoredTrace.EnrichmentTrace.ResolvedEpisode);
        Assert.Equal("Episode", restoredTrace.EnrichmentTrace.ResolvedTitle);
        Assert.Equal(2026, restoredTrace.EnrichmentTrace.ResolvedYear);
    }

    [Fact]
    public void ItemTrace_Roundtrip_WithNullOptionalFields()
    {
        var candidate = new ScoreCandidate("Title", "Topic", "ZDF", 1800, 480, null, _baseTime.ToUnixTimeSeconds());
        var trace = new ItemTrace(candidate, false, 0, null, null, [], null);
        var cmd = new RecordHistory(Guid.NewGuid(), "rs-1",
            new ScoringOrigin(SearchSource.Radarr, "query"), _baseTime, 1, 0, 0, [trace]);

        var (state, _) = HistoryState.Empty.ProcessCommand(cmd);
        var restored = HistoryState.FromPersistence(state.GetPersistenceState());

        var entry = Assert.Single(restored.Snapshots);
        var restoredTrace = Assert.Single(entry.ItemTraces);

        Assert.False(restoredTrace.Matched);
        Assert.Equal(0, restoredTrace.Score);
        Assert.Null(restoredTrace.MatchedRuleId);
        Assert.Null(restoredTrace.Identification);
        Assert.Empty(restoredTrace.RuleTraces);
        Assert.Null(restoredTrace.EnrichmentTrace);
    }

    [Fact]
    public void FilterGroupTrace_NestedGroup_Roundtrip()
    {
        var innerNode = new FilterNodeTrace("channel", "equals", "ARD", "ARD", true, false, null);
        var innerGroup = new FilterGroupTrace(FilterGroupOp.Any, true, [innerNode]);
        var outerNode = new FilterNodeTrace("title", "contains", "test", "no match", false, false, innerGroup);
        var outerGroup = new FilterGroupTrace(FilterGroupOp.All, false, [outerNode]);

        var ruleTrace = new RuleTrace("rule-1", 0, RuleOutcome.FilterFailed, outerGroup, null);
        var candidate = new ScoreCandidate("Title", "Topic", "ARD", 0, 720, null, _baseTime.ToUnixTimeSeconds());
        var trace = new ItemTrace(candidate, false, 0, null, null, [ruleTrace], null);
        var cmd = new RecordHistory(Guid.NewGuid(), "rs-1",
            new ScoringOrigin(SearchSource.Test, "query"), _baseTime, 1, 0, 0, [trace]);

        var (state, _) = HistoryState.Empty.ProcessCommand(cmd);
        var restored = HistoryState.FromPersistence(state.GetPersistenceState());

        var entry = Assert.Single(restored.Snapshots);
        var restoredRule = Assert.Single(entry.ItemTraces[0].RuleTraces);

        Assert.NotNull(restoredRule.FilterTrace);
        Assert.Equal(FilterGroupOp.All, restoredRule.FilterTrace.Operator);
        Assert.False(restoredRule.FilterTrace.Passed);

        var node = Assert.Single(restoredRule.FilterTrace.Nodes);
        Assert.NotNull(node.Group);
        Assert.Equal(FilterGroupOp.Any, node.Group.Operator);
        Assert.True(node.Group.Passed);
    }

    [Fact]
    public void IdentificationTrace_NullStrategyAndDetail_Roundtrip()
    {
        var idTrace = new IdentificationTrace(null, false, null);
        var ruleTrace = new RuleTrace("rule-1", 0, RuleOutcome.Matched, null, idTrace);
        var candidate = new ScoreCandidate("Title", "Topic", "ARD", 0, 720, null, _baseTime.ToUnixTimeSeconds());
        var trace = new ItemTrace(candidate, true, 50, "rule-1", null, [ruleTrace], null);
        var cmd = new RecordHistory(Guid.NewGuid(), "rs-1",
            new ScoringOrigin(SearchSource.Sonarr, "query"), _baseTime, 1, 1, 0, [trace]);

        var (state, _) = HistoryState.Empty.ProcessCommand(cmd);
        var restored = HistoryState.FromPersistence(state.GetPersistenceState());

        var restoredRule = Assert.Single(restored.Snapshots[0].ItemTraces[0].RuleTraces);

        Assert.NotNull(restoredRule.IdentificationTrace);
        Assert.Null(restoredRule.IdentificationTrace.Strategy);
        Assert.False(restoredRule.IdentificationTrace.Attempted);
        Assert.Null(restoredRule.IdentificationTrace.Detail);
    }

    [Fact]
    public void EnrichmentTrace_WithDaysDiffAndDetail_Roundtrip()
    {
        var enrichment = new EnrichmentTrace(MatchMethod.AirdateMatch, 0.7f, false, null, null, null, null, 5, "Too far apart");
        var candidate = new ScoreCandidate("Title", "Topic", "SRF", 0, 720, null, _baseTime.ToUnixTimeSeconds());
        var trace = new ItemTrace(candidate, false, 0, null, null, [], enrichment);
        var cmd = new RecordHistory(Guid.NewGuid(), "rs-1",
            new ScoringOrigin(SearchSource.Prowlarr, "query"), _baseTime, 1, 0, 0, [trace]);

        var (state, _) = HistoryState.Empty.ProcessCommand(cmd);
        var restored = HistoryState.FromPersistence(state.GetPersistenceState());

        var restoredTrace = Assert.Single(restored.Snapshots[0].ItemTraces);

        Assert.NotNull(restoredTrace.EnrichmentTrace);
        Assert.Equal(MatchMethod.AirdateMatch, restoredTrace.EnrichmentTrace.Method);
        Assert.Equal(0.7f, restoredTrace.EnrichmentTrace.Confidence);
        Assert.False(restoredTrace.EnrichmentTrace.Enriched);
        Assert.Null(restoredTrace.EnrichmentTrace.ResolvedSeason);
        Assert.Null(restoredTrace.EnrichmentTrace.ResolvedEpisode);
        Assert.Null(restoredTrace.EnrichmentTrace.ResolvedTitle);
        Assert.Null(restoredTrace.EnrichmentTrace.ResolvedYear);
        Assert.Equal(5, restoredTrace.EnrichmentTrace.DaysDiff);
        Assert.Equal("Too far apart", restoredTrace.EnrichmentTrace.Detail);
    }
}
