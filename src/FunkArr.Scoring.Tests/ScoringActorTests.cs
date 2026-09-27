using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Messages;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringActorTests : TestKit
{
    private static MatchingConfig Config(float confidence, params MatchingRule[] rules) =>
        new("test", confidence, rules);

    private static ScoreCandidate Candidate(
        string title = "Tatort: Die goldene Zeit",
        string topic = "Tatort",
        string channel = "ARD",
        int durationSeconds = 5400,
        int quality = 720) =>
        new(title, topic, channel, durationSeconds, quality, null, 0);

    private static FilterNode Condition(FilterField field, FilterOp op, string value) =>
        new FilterNode.ConditionNode(new FilterCondition(field, op, value));

    private ScoreCompleted Score(MatchingConfig config, params ScoreCandidate[] items)
    {
        var actor = Sys.ActorOf(Props.Create<ScoringActor>());
        actor.Tell(new ExecuteScoring(config, items, Guid.Empty, new ScoringOrigin(SearchSource.Sonarr, "test")));
        return ExpectMsg<ScoreCompleted>();
    }

    [Fact]
    public void No_rules_returns_unmatched()
    {
        var result = Score(Config(0.9f), Candidate());

        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void Filter_all_conditions_must_pass()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [
                Condition(FilterField.Duration, FilterOp.GreaterThan, "60"),
                Condition(FilterField.Channel, FilterOp.Eq, "ARD"),
            ]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));

        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void Filter_all_fails_if_one_misses()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [
                Condition(FilterField.Duration, FilterOp.GreaterThan, "60"),
                Condition(FilterField.Channel, FilterOp.Eq, "ZDF"),
            ]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024", channel: "ARD"));

        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void Filter_any_passes_if_one_matches()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(Any: [
                Condition(FilterField.Channel, FilterOp.Eq, "ZDF"),
                Condition(FilterField.Channel, FilterOp.Eq, "ARD"),
            ]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));

        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void Filter_any_fails_if_none_match()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(Any: [
                Condition(FilterField.Channel, FilterOp.Eq, "ZDF"),
                Condition(FilterField.Channel, FilterOp.Eq, "BR"),
            ]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));

        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void Filter_not_blocks_matching()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(Not: [
                Condition(FilterField.Title, FilterOp.Contains, "Audiodeskription"),
            ]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var pass = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));
        var passItem = Assert.Single(pass.Results);
        Assert.True(passItem.Matched);

        var blocked = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024 (Audiodeskription)"));
        var blockedItem = Assert.Single(blocked.Results);
        Assert.False(blockedItem.Matched);
    }

    [Fact]
    public void Filter_nested_group()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [
                Condition(FilterField.Duration, FilterOp.GreaterThan, "30"),
                new FilterNode.GroupNode(new FilterSpec(Any: [
                    Condition(FilterField.Channel, FilterOp.Eq, "ARD"),
                    Condition(FilterField.Channel, FilterOp.Eq, "ZDF"),
                ])),
            ]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));
        var resultItem = Assert.Single(result.Results);
        Assert.True(resultItem.Matched);

        var brResult = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024", channel: "BR"));
        var brItem = Assert.Single(brResult.Results);
        Assert.False(brItem.Matched);
    }

    [Fact]
    public void Filter_duration_compared_in_minutes()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [Condition(FilterField.Duration, FilterOp.GreaterThan, "60")]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var pass = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024", durationSeconds: 5400));
        var passItem = Assert.Single(pass.Results);
        Assert.True(passItem.Matched);

        var fail = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024", durationSeconds: 1800));
        var failItem = Assert.Single(fail.Results);
        Assert.False(failItem.Matched);
    }

    [Fact]
    public void Filter_contains_case_insensitive()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [Condition(FilterField.Title, FilterOp.Contains, "GOLDENE")]),
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.*)", Field: FilterField.Title)]));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void Filter_regex_passes()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [Condition(FilterField.Title, FilterOp.Regex, "^Tatort")]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var pass = Score(Config(0.5f, rule), Candidate(title: "Tatort vom 24.10.2024"));
        var passItem = Assert.Single(pass.Results);
        Assert.True(passItem.Matched);

        var fail = Score(Config(0.5f, rule), Candidate(title: "heute-show vom 24.10.2024"));
        var failItem = Assert.Single(fail.Results);
        Assert.False(failItem.Matched);
    }

    [Fact]
    public void Filter_not_contains()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [Condition(FilterField.Title, FilterOp.NotContains, "Trailer")]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var pass = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));
        var passItem = Assert.Single(pass.Results);
        Assert.True(passItem.Matched);

        var fail = Score(Config(0.5f, rule), Candidate(title: "Trailer vom 24.10.2024"));
        var failItem = Assert.Single(fail.Results);
        Assert.False(failItem.Matched);
    }

    [Fact]
    public void Null_filters_passes_all()
    {
        var rule = new MatchingRule("r1", 0, 0.9f, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void RegexCapture_season_and_episode()
    {
        var rule = new MatchingRule("r1", 0, 0.95f, null,
            new IdentificationSpec(IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: @"(?<=S)(\d{2,4})(?=/E)",
                EpisodePattern: @"(?<=E)(\d{2,4})(?=\))"));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort (S01/E05)"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
        Assert.Equal(0.95, item.Score, 0.001);
    }

    [Fact]
    public void RegexCapture_season_and_episode_no_match()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: @"(?<=S)(\d{2,4})",
                EpisodePattern: @"(?<=E)(\d{2,4})"));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void RegexCapture_absolute_episode_only()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.AbsoluteEpisodeNumber,
                EpisodePattern: @"Folge\s*(\d+)"));

        var result = Score(Config(0.9f, rule), Candidate(title: "Löwenzahn - Folge 312"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
        Assert.Equal(0.9, item.Score, 0.001);
    }

    [Fact]
    public void RegexCapture_absolute_episode_no_match()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.AbsoluteEpisodeNumber,
                EpisodePattern: @"Folge\s*(\d+)"));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void RegexCapture_explicit_capture_group()
    {
        var rule = new MatchingRule("r1", 0, 0.9f, null,
            new IdentificationSpec(IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: @"(S)(\d{2})",
                EpisodePattern: @"(E)(\d{2})",
                CaptureGroup: 2));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort S01E05"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void TitleConstruction_exact_match()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.*)", Field: FilterField.Title)]));

        var result = Score(Config(0.9f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void TitleConstruction_exact_no_match()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [new TitlePart(TitlePartType.Static, Value: "Schwarzer Freitag")]));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void TitleConstruction_static_and_regex_parts()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [
                    new TitlePart(TitlePartType.Static, Value: "Folge 42"),
                ]));

        var result = Score(Config(0.5f, rule), Candidate(title: "Folge 42"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void TitleConstruction_chain_with_static_separator()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [
                    new TitlePart(TitlePartType.Regex, Pattern: @"^(\w+):", Field: FilterField.Title, CaptureGroup: 1),
                    new TitlePart(TitlePartType.Static, Value: " & "),
                    new TitlePart(TitlePartType.Regex, Pattern: @"^(\w+)", Field: FilterField.Topic),
                ]));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort & Krimi", topic: "Krimi"));
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);

        var result2 = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit", topic: "Krimi"));
        var item2 = Assert.Single(result2.Results);
        Assert.False(item2.Matched);
    }

    [Fact]
    public void TitleConstruction_regex_extraction_fails_returns_no_match()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [
                    new TitlePart(TitlePartType.Regex, Pattern: @"NOMATCH_(\d+)", Field: FilterField.Title),
                ]));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void TitleConstruction_contains_mode()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleIncludes,
                TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @":\s*(.+)", Field: FilterField.Title)]));

        var result = Score(Config(0.9f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void TitleConstruction_contains_no_match()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleIncludes,
                TitleParts: [new TitlePart(TitlePartType.Static, Value: "Schwarzer Freitag")]));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void TitleConstruction_contains_umlaut_normalized()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.TitleIncludes,
                TitleParts: [new TitlePart(TitlePartType.Static, Value: "Löwenzähn")]));

        var result = Score(Config(0.9f, rule), Candidate(title: "Löwenzähn - Folge 312"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void AirdateExtraction_numeric_date()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.9f, rule), Candidate(title: "heute-show vom 24.10.2024"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
        Assert.Equal(0.9, item.Score, 0.001);
    }

    [Fact]
    public void AirdateExtraction_two_digit_year()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.9f, rule), Candidate(title: "Sendung vom 24.10.24"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void AirdateExtraction_german_month()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.9f, rule), Candidate(title: "heute-show vom 16. Juli 2024"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
    }

    [Fact]
    public void AirdateExtraction_no_date_no_match()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void Priority_ordering_lower_wins()
    {
        var rule0 = new MatchingRule("r0", 0, 0.95f, null,
            new IdentificationSpec(IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: @"(?<=S)(\d{2,4})(?=/E)",
                EpisodePattern: @"(?<=E)(\d{2,4})(?=\))"));

        var rule1 = new MatchingRule("r1", 10, 0.7f, null,
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.*)", Field: FilterField.Title)]));

        var result = Score(Config(0.5f, rule0, rule1), Candidate(title: "Tatort (S01/E05)"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
        Assert.Equal(0.95, item.Score, 0.001);
    }

    [Fact]
    public void Priority_fallback_to_higher()
    {
        var rule0 = new MatchingRule("r0", 0, 0.95f, null,
            new IdentificationSpec(IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: @"(?<=S)(\d{2,4})(?=/E)",
                EpisodePattern: @"(?<=E)(\d{2,4})(?=\))"));

        var rule1 = new MatchingRule("r1", 10, 0.7f, null,
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.*)", Field: FilterField.Title)]));

        var result = Score(Config(0.5f, rule0, rule1), Candidate(title: "Tatort: Die goldene Zeit"));
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
        Assert.Equal(0.7, item.Score, 0.001);
    }

    [Fact]
    public void Confidence_rule_overrides_default()
    {
        var rule = new MatchingRule("r1", 0, 0.95f, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));
        var item = Assert.Single(result.Results);
        Assert.Equal(0.95, item.Score, 0.001);
    }

    [Fact]
    public void Confidence_null_uses_default()
    {
        var rule = new MatchingRule("r1", 0, null, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.85f, rule), Candidate(title: "Sendung vom 24.10.2024"));
        var item = Assert.Single(result.Results);
        Assert.Equal(0.85, item.Score, 0.001);
    }

    [Fact]
    public void Multiple_items_scored_independently()
    {
        var rule = new MatchingRule("r1", 0, 0.9f, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = Score(Config(0.5f, rule),
            Candidate(title: "Sendung vom 24.10.2024"),
            Candidate(title: "No Date Here"),
            Candidate(title: "Sendung vom 16. Juli 2024"));

        Assert.Equal(3, result.Results.Length);
        Assert.True(result.Results[0].Matched);
        Assert.False(result.Results[1].Matched);
        Assert.True(result.Results[2].Matched);
    }

    [Fact]
    public void Filter_with_all_and_not_combined()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(
                All: [Condition(FilterField.Duration, FilterOp.GreaterThan, "30")],
                Not: [Condition(FilterField.Title, FilterOp.Contains, "Trailer")]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var pass = Score(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));
        var passItem = Assert.Single(pass.Results);
        Assert.True(passItem.Matched);

        var fail = Score(Config(0.5f, rule), Candidate(title: "Trailer vom 24.10.2024"));
        var failItem = Assert.Single(fail.Results);
        Assert.False(failItem.Matched);
    }

    private ScoreCompleted ScoreWithTrace(MatchingConfig config, params ScoreCandidate[] items)
    {
        var actor = Sys.ActorOf(Props.Create<ScoringActor>());
        actor.Tell(new ExecuteScoring(config, items, Guid.NewGuid(), new ScoringOrigin(SearchSource.Sonarr, "test")));
        return ExpectMsg<ScoreCompleted>();
    }

    [Fact]
    public void Trace_matched_item_has_outcome_matched()
    {
        var rule = new MatchingRule("airdate", 0, 0.9f,
            new FilterSpec(All: [Condition(FilterField.Channel, FilterOp.Eq, "ARD")]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = ScoreWithTrace(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024"));

        var item = Assert.Single(result.ItemTraces);
        Assert.True(item.Matched);
        Assert.Equal("airdate", item.MatchedRuleId);
        Assert.NotNull(item.Identification);
        Assert.Equal("2024-10-24", item.Identification.Title);

        var traceItem = Assert.Single(item.RuleTraces);
        Assert.Equal(RuleOutcome.Matched, traceItem.Outcome);
        Assert.NotNull(traceItem.FilterTrace);
        Assert.True(traceItem.FilterTrace.Passed);
        Assert.NotNull(traceItem.IdentificationTrace);
        Assert.True(traceItem.IdentificationTrace.Attempted);
        Assert.Null(traceItem.IdentificationTrace.Detail);
    }

    [Fact]
    public void Trace_unmatched_item_filter_failed_shows_condition()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [Condition(FilterField.Channel, FilterOp.Eq, "ZDF")]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = ScoreWithTrace(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024", channel: "ARD"));

        var traceItem = Assert.Single(result.ItemTraces);
        Assert.False(traceItem.Matched);
        var ruleItem = Assert.Single(traceItem.RuleTraces);
        Assert.Equal(RuleOutcome.FilterFailed, ruleItem.Outcome);

        Assert.NotNull(ruleItem.FilterTrace);
        Assert.False(ruleItem.FilterTrace.Passed);
        var filterNodeItem = Assert.Single(ruleItem.FilterTrace.Nodes);
        Assert.Equal("Channel", filterNodeItem.Field);
        Assert.Equal("Eq", filterNodeItem.Op);
        Assert.Equal("ZDF", filterNodeItem.ExpectedValue);
        Assert.Equal("ARD", filterNodeItem.ActualValue);
        Assert.False(filterNodeItem.Passed);
    }

    [Fact]
    public void Trace_short_circuit_marks_skipped()
    {
        var rule = new MatchingRule("r1", 0, 0.9f,
            new FilterSpec(All: [
                Condition(FilterField.Channel, FilterOp.Eq, "ZDF"),
                Condition(FilterField.Duration, FilterOp.GreaterThan, "30"),
            ]),
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = ScoreWithTrace(Config(0.5f, rule), Candidate(title: "Sendung vom 24.10.2024", channel: "ARD"));

        var traceItem = Assert.Single(result.ItemTraces);
        var ruleTrace = Assert.Single(traceItem.RuleTraces);
        Assert.NotNull(ruleTrace.FilterTrace);
        Assert.Equal(2, ruleTrace.FilterTrace.Nodes.Length);
        Assert.False(ruleTrace.FilterTrace.Nodes[0].Passed);
        Assert.False(ruleTrace.FilterTrace.Nodes[0].Skipped);
        Assert.True(ruleTrace.FilterTrace.Nodes[1].Skipped);
        Assert.Null(ruleTrace.FilterTrace.Nodes[1].ActualValue);
    }

    [Fact]
    public void Trace_identification_failed_has_detail()
    {
        var rule = new MatchingRule("r1", 0, 0.9f, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = ScoreWithTrace(Config(0.5f, rule), Candidate(title: "Tatort: Die goldene Zeit"));

        var traceItem = Assert.Single(result.ItemTraces);
        var ruleTrace = Assert.Single(traceItem.RuleTraces);
        Assert.Equal(RuleOutcome.IdentificationFailed, ruleTrace.Outcome);
        Assert.NotNull(ruleTrace.IdentificationTrace);
        Assert.True(ruleTrace.IdentificationTrace.Attempted);
        Assert.Equal(IdentificationFailureReason.NoDateFoundInTitle, ruleTrace.IdentificationTrace.Detail);
    }

    [Fact]
    public void Trace_multiple_rules_shows_fallthrough()
    {
        var rule0 = new MatchingRule("season-ep", 0, 0.95f, null,
            new IdentificationSpec(IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: @"(?<=S)(\d{2,4})",
                EpisodePattern: @"(?<=E)(\d{2,4})"));

        var rule1 = new MatchingRule("airdate", 10, 0.7f, null,
            new IdentificationSpec(IdentificationStrategy.AirdateExtraction));

        var result = ScoreWithTrace(Config(0.5f, rule0, rule1),
            Candidate(title: "Sendung vom 24.10.2024"));

        var traceItem = Assert.Single(result.ItemTraces);
        Assert.True(traceItem.Matched);
        Assert.Equal("airdate", traceItem.MatchedRuleId);
        Assert.Equal(2, traceItem.RuleTraces.Length);
        Assert.Equal(RuleOutcome.IdentificationFailed, traceItem.RuleTraces[0].Outcome);
        Assert.Equal(RuleOutcome.Matched, traceItem.RuleTraces[1].Outcome);
    }

}
