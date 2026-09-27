using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringEngineTests
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

    private static MatchingRule ChannelRule(string channel, string id = "rule-1", int priority = 0) =>
        new(id, priority, null,
            new FilterSpec(
                [new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, channel))]),
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.+)")]));

    [Fact]
    public void Score_with_matching_rule_returns_matched()
    {
        var config = Config(0.9f, ChannelRule("ARD"));
        var candidates = new[] { Candidate() };

        var (scored, traces) = ScoringEngine.Score(config, candidates);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
        Assert.Equal(0.9, item.Score, 2);
        var item2 = Assert.Single(traces);
        Assert.True(item2.Matched);
    }

    [Fact]
    public void Score_with_no_matching_rule_returns_unmatched()
    {
        var config = Config(0.9f, ChannelRule("ZDF"));
        var candidates = new[] { Candidate() };

        var (scored, traces) = ScoringEngine.Score(config, candidates);

        var item = Assert.Single(scored);
        Assert.False(item.Matched);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void Score_with_empty_candidates_returns_empty()
    {
        var config = Config(0.9f, ChannelRule("ARD"));

        var (scored, traces) = ScoringEngine.Score(config, []);

        Assert.Empty(scored);
        Assert.Empty(traces);
    }

    [Fact]
    public void Score_applies_rules_in_priority_order()
    {
        var config = Config(0.5f,
            new MatchingRule("low-priority", 10, 0.3f,
                new FilterSpec(
                    [new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ARD"))]),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.+)")])),
            new MatchingRule("high-priority", 0, 0.95f,
                new FilterSpec(
                    [new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ARD"))]),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.+)")])));

        var (scored, traces) = ScoringEngine.Score(config, [Candidate()]);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
        Assert.Equal(0.95, item.Score, 2);
        var trace = Assert.Single(traces);
        Assert.Equal("high-priority", trace.MatchedRuleId);
    }

    [Fact]
    public void Filter_all_group_short_circuits_on_failure()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null,
                new FilterSpec(
                    [
                        new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ZDF")),
                        new FilterNode.ConditionNode(new FilterCondition(FilterField.Topic, FilterOp.Eq, "Tatort")),
                        new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ARD")),
                    ]),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.+)")])));

        var (_, traces) = ScoringEngine.Score(config, [Candidate()]);

        var traceItem = Assert.Single(traces);
        var ruleTrace = Assert.Single(traceItem.RuleTraces);
        Assert.Equal(RuleOutcome.FilterFailed, ruleTrace.Outcome);
        Assert.NotNull(ruleTrace.FilterTrace);
        var filterNodes = ruleTrace.FilterTrace.Nodes;
        Assert.Equal(3, filterNodes.Length);
        Assert.False(filterNodes[0].Passed);
        Assert.True(filterNodes[1].Skipped);
        Assert.True(filterNodes[2].Skipped);
    }

    [Fact]
    public void Filter_any_group_short_circuits_on_success()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null,
                new FilterSpec(null,
                    [
                        new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ARD")),
                        new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ZDF")),
                    ]),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.+)")])));

        var (scored, traces) = ScoringEngine.Score(config, [Candidate()]);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
        var traceItem = Assert.Single(traces);
        var ruleTrace = Assert.Single(traceItem.RuleTraces);
        Assert.NotNull(ruleTrace.FilterTrace);
        var filterNodes = ruleTrace.FilterTrace.Nodes;
        Assert.Equal(2, filterNodes.Length);
        Assert.True(filterNodes[0].Passed);
        Assert.True(filterNodes[1].Skipped);
    }

    [Fact]
    public void Filter_not_group_excludes_matching_candidates()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null,
                new FilterSpec(null, null,
                    [new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ARD"))]),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.+)")])));

        var (scored, _) = ScoringEngine.Score(config, [Candidate()]);

        var item = Assert.Single(scored);
        Assert.False(item.Matched);
    }

    [Fact]
    public void Identification_season_and_episode_extracts_numbers()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.SeasonAndEpisodeNumber,
                    SeasonPattern: @"S(\d+)", EpisodePattern: @"E(\d+)")));

        var (scored, traces) = ScoringEngine.Score(config,
            [Candidate(title: "Show S02E05 - Title")]);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
        var trace = Assert.Single(traces);
        Assert.NotNull(trace.Identification);
        Assert.Equal("02", trace.Identification.Season);
        Assert.Equal("05", trace.Identification.Episode);
    }

    [Fact]
    public void Identification_absolute_episode_extracts_number()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AbsoluteEpisodeNumber,
                    EpisodePattern: @"Folge\s+(\d+)")));

        var (scored, traces) = ScoringEngine.Score(config,
            [Candidate(title: "Show Folge 42")]);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
        var trace = Assert.Single(traces);
        Assert.NotNull(trace.Identification);
        Assert.Null(trace.Identification.Season);
        Assert.Equal("42", trace.Identification.Episode);
    }

    [Fact]
    public void Identification_title_includes_matches_substring()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.TitleIncludes,
                    TitleParts: [new TitlePart(TitlePartType.Static, Value: "goldene Zeit")])));

        var (scored, _) = ScoringEngine.Score(config, [Candidate()]);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
    }

    [Fact]
    public void Identification_airdate_extracts_german_date()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AirdateExtraction)));

        var (scored, traces) = ScoringEngine.Score(config,
            [Candidate(title: "Show vom 25.06.2024")]);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
        var trace = Assert.Single(traces);
        Assert.NotNull(trace.Identification);
        Assert.Equal("2024-06-25", trace.Identification.Title);
    }

    [Fact]
    public void TitleParts_preserves_constructed_title_in_metadata()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.TitleIncludes,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @":\s*(.+)")])));

        var (scored, _) = ScoringEngine.Score(config,
            [Candidate(title: "Tatort: Roomservice")]);

        var item = Assert.Single(scored);
        Assert.True(item.Matched);
        Assert.Equal("Roomservice", item.Metadata?.ConstructedTitle);
    }

    [Fact]
    public void Regex_timeout_returns_gracefully()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null,
                new FilterSpec(
                    [new FilterNode.ConditionNode(new FilterCondition(
                        FilterField.Title, FilterOp.Regex, "(a+)+$"))]),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: "(.+)")])));

        var pathologicalInput = new string('a', 30) + "!";
        var (scored, _) = ScoringEngine.Score(config, [Candidate(title: pathologicalInput)]);

        var item = Assert.Single(scored);
        Assert.False(item.Matched);
    }
}
