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
                [new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, channel))],
                null, null),
            new IdentificationSpec(IdentificationStrategy.TitleExact,
                TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @"(.+)")]));

    [Fact]
    public void Score_with_matching_rule_returns_matched()
    {
        var config = Config(0.9f, ChannelRule("ARD"));
        var candidates = new[] { Candidate() };

        var (scored, traces) = ScoringEngine.Score(config, candidates);

        Assert.Single(scored);
        Assert.True(scored[0].Matched);
        Assert.Equal(0.9, scored[0].Score, 2);
        Assert.Single(traces);
        Assert.True(traces[0].Matched);
    }

    [Fact]
    public void Score_with_no_matching_rule_returns_unmatched()
    {
        var config = Config(0.9f, ChannelRule("ZDF"));
        var candidates = new[] { Candidate() };

        var (scored, traces) = ScoringEngine.Score(config, candidates);

        Assert.Single(scored);
        Assert.False(scored[0].Matched);
        Assert.Equal(0.0, scored[0].Score);
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
                    [new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ARD"))],
                    null, null),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @"(.+)")])),
            new MatchingRule("high-priority", 0, 0.95f,
                new FilterSpec(
                    [new FilterNode.ConditionNode(new FilterCondition(FilterField.Channel, FilterOp.Eq, "ARD"))],
                    null, null),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @"(.+)")])));

        var (scored, traces) = ScoringEngine.Score(config, [Candidate()]);

        Assert.True(scored[0].Matched);
        Assert.Equal(0.95, scored[0].Score, 2);
        Assert.Equal("high-priority", traces[0].MatchedRuleId);
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
                    ],
                    null, null),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @"(.+)")])));

        var (_, traces) = ScoringEngine.Score(config, [Candidate()]);

        var ruleTrace = traces[0].RuleTraces[0];
        Assert.Equal(RuleOutcome.FilterFailed, ruleTrace.Outcome);
        var filterNodes = ruleTrace.FilterTrace!.Nodes;
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
                    ],
                    null),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @"(.+)")])));

        var (scored, traces) = ScoringEngine.Score(config, [Candidate()]);

        Assert.True(scored[0].Matched);
        var filterNodes = traces[0].RuleTraces[0].FilterTrace!.Nodes;
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
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @"(.+)")])));

        var (scored, _) = ScoringEngine.Score(config, [Candidate()]);

        Assert.False(scored[0].Matched);
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

        Assert.True(scored[0].Matched);
        Assert.Equal("02", traces[0].Identification!.Season);
        Assert.Equal("05", traces[0].Identification!.Episode);
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

        Assert.True(scored[0].Matched);
        Assert.Null(traces[0].Identification!.Season);
        Assert.Equal("42", traces[0].Identification!.Episode);
    }

    [Fact]
    public void Identification_title_includes_matches_substring()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.TitleIncludes,
                    TitleParts: [new TitlePart(TitlePartType.Static, Value: "goldene Zeit")])));

        var (scored, _) = ScoringEngine.Score(config, [Candidate()]);

        Assert.True(scored[0].Matched);
    }

    [Fact]
    public void Identification_airdate_extracts_german_date()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AirdateExtraction)));

        var (scored, traces) = ScoringEngine.Score(config,
            [Candidate(title: "Show vom 25.06.2024")]);

        Assert.True(scored[0].Matched);
        Assert.Equal("2024-06-25", traces[0].Identification!.Title);
    }

    [Fact]
    public void Regex_timeout_returns_gracefully()
    {
        var config = Config(0.9f,
            new MatchingRule("r1", 0, null,
                new FilterSpec(
                    [new FilterNode.ConditionNode(new FilterCondition(
                        FilterField.Title, FilterOp.Regex, @"(a+)+$"))],
                    null, null),
                new IdentificationSpec(IdentificationStrategy.TitleExact,
                    TitleParts: [new TitlePart(TitlePartType.Regex, Pattern: @"(.+)")])));

        var pathologicalInput = new string('a', 30) + "!";
        var (scored, _) = ScoringEngine.Score(config, [Candidate(title: pathologicalInput)]);

        Assert.False(scored[0].Matched);
    }
}
