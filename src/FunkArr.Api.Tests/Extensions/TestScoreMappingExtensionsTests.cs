using FunkArr.Api.Extensions;
using FunkArr.Api.Models;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Tests.Extensions;

public sealed class TestScoreMappingExtensionsTests
{
    [Fact]
    public void ToMessage_creates_config_with_test_ruleset_id()
    {
        var request = new TestScoreRequest(
            0.8f,
            [new RuleInput("r1", Strategy: IdentificationStrategy.AirdateExtraction)],
            [new TestCandidate("Title")]);

        var (config, _) = request.ToMessage();

        Assert.Equal("test", config.RuleSetId);
    }

    [Fact]
    public void ToMessage_maps_default_confidence()
    {
        var request = new TestScoreRequest(
            0.65f,
            [new RuleInput("r1", Strategy: IdentificationStrategy.AirdateExtraction)],
            [new TestCandidate("Title")]);

        var (config, _) = request.ToMessage();

        Assert.Equal(0.65f, config.DefaultConfidence);
    }

    [Fact]
    public void ToMessage_maps_candidates()
    {
        var request = new TestScoreRequest(
            0.8f,
            [new RuleInput("r1", Strategy: IdentificationStrategy.AirdateExtraction)],
            [new TestCandidate("Tagesschau", "News", "ARD", 1800, 720, "Daily news", 1725300000)]);

        var (_, candidates) = request.ToMessage();

        var candidate = Assert.Single(candidates);
        Assert.Equal("Tagesschau", candidate.Title);
        Assert.Equal("News", candidate.Topic);
        Assert.Equal("ARD", candidate.Channel);
        Assert.Equal(1800, candidate.Duration);
        Assert.Equal(720, candidate.Quality);
        Assert.Equal("Daily news", candidate.Description);
        Assert.Equal(1725300000, candidate.Timestamp);
    }

    [Fact]
    public void ToMessage_filters_out_rules_with_null_strategy()
    {
        var request = new TestScoreRequest(
            0.8f,
            [
                new RuleInput("r1", Strategy: null),
                new RuleInput("r2", Strategy: IdentificationStrategy.AirdateExtraction)
            ],
            [new TestCandidate("Title")]);

        var (config, _) = request.ToMessage();

        var rule = Assert.Single(config.Rules);
        Assert.Equal("r2", rule.Id);
    }

    [Fact]
    public void RuleInput_maps_season_and_episode_strategy()
    {
        var rule = new RuleInput(
            "r1",
            Priority: 5,
            Confidence: 0.9f,
            Strategy: IdentificationStrategy.SeasonAndEpisodeNumber,
            SeasonRegex: @"Staffel (\d+)",
            EpisodeRegex: @"Folge (\d+)",
            CaptureGroup: 1);

        var result = rule.ToMessage();

        Assert.NotNull(result);
        Assert.Equal("r1", result.Id);
        Assert.Equal(5, result.Priority);
        Assert.Equal(0.9f, result.Confidence);
        Assert.Equal(MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber, result.Identification.Strategy);
        Assert.Equal(@"Staffel (\d+)", result.Identification.SeasonPattern);
        Assert.Equal(@"Folge (\d+)", result.Identification.EpisodePattern);
        Assert.Equal(1, result.Identification.CaptureGroup);
    }

    [Fact]
    public void RuleInput_maps_absolute_episode_strategy()
    {
        var rule = new RuleInput(
            "r1",
            Strategy: IdentificationStrategy.AbsoluteEpisodeNumber,
            EpisodeRegex: @"(\d+)",
            CaptureGroup: 1);

        var result = rule.ToMessage();

        Assert.NotNull(result);
        Assert.Equal(MsgScoring.IdentificationStrategy.AbsoluteEpisodeNumber, result.Identification.Strategy);
        Assert.Equal(@"(\d+)", result.Identification.EpisodePattern);
        Assert.Equal(1, result.Identification.CaptureGroup);
        Assert.Null(result.Identification.SeasonPattern);
    }

    [Fact]
    public void RuleInput_maps_title_exact_with_title_rules()
    {
        var rule = new RuleInput(
            "r1",
            Strategy: IdentificationStrategy.TitleExact,
            TitleRules:
            [
                new TitleRuleInput(TitlePartType.Static, Value: "Tagesschau"),
                new TitleRuleInput(TitlePartType.Regex, Field: FilterField.Title, Pattern: @"(\d+)", CaptureGroup: 1)
            ]);

        var result = rule.ToMessage();

        Assert.NotNull(result);
        Assert.Equal(MsgScoring.IdentificationStrategy.TitleExact, result.Identification.Strategy);
        Assert.NotNull(result.Identification.TitleParts);
        Assert.Equal(2, result.Identification.TitleParts.Length);
        Assert.Equal(MsgScoring.TitlePartType.Static, result.Identification.TitleParts[0].Type);
        Assert.Equal("Tagesschau", result.Identification.TitleParts[0].Value);
        Assert.Equal(MsgScoring.TitlePartType.Regex, result.Identification.TitleParts[1].Type);
        Assert.Equal(MsgScoring.FilterField.Title, result.Identification.TitleParts[1].Field);
        Assert.Equal(@"(\d+)", result.Identification.TitleParts[1].Pattern);
        Assert.Equal(1, result.Identification.TitleParts[1].CaptureGroup);
    }

    [Fact]
    public void RuleInput_maps_airdate_extraction_strategy()
    {
        var rule = new RuleInput("r1", Strategy: IdentificationStrategy.AirdateExtraction);

        var result = rule.ToMessage();

        Assert.NotNull(result);
        Assert.Equal(MsgScoring.IdentificationStrategy.AirdateExtraction, result.Identification.Strategy);
        Assert.Null(result.Identification.SeasonPattern);
        Assert.Null(result.Identification.EpisodePattern);
        Assert.Null(result.Identification.TitleParts);
    }

    [Fact]
    public void RuleInput_maps_filters_with_conditions()
    {
        var rule = new RuleInput(
            "r1",
            Strategy: IdentificationStrategy.AirdateExtraction,
            Filters: new FilterGroupInput(
                All:
                [
                    new FilterNodeInput(Field: FilterField.Channel, Op: FilterOp.Eq, Value: "ARD"),
                    new FilterNodeInput(Field: FilterField.Duration, Op: FilterOp.GreaterThan, Value: "300")
                ]));

        var result = rule.ToMessage();

        Assert.NotNull(result);
        Assert.NotNull(result.Filters);
        Assert.NotNull(result.Filters.All);
        Assert.Equal(2, result.Filters.All.Length);

        var node0 = Assert.IsType<MsgScoring.FilterNode.ConditionNode>(result.Filters.All[0]);
        Assert.Equal(MsgScoring.FilterField.Channel, node0.Condition.Field);
        Assert.Equal(MsgScoring.FilterOp.Eq, node0.Condition.Op);
        Assert.Equal("ARD", node0.Condition.Value);

        var node1 = Assert.IsType<MsgScoring.FilterNode.ConditionNode>(result.Filters.All[1]);
        Assert.Equal(MsgScoring.FilterField.Duration, node1.Condition.Field);
        Assert.Equal(MsgScoring.FilterOp.GreaterThan, node1.Condition.Op);
        Assert.Equal("300", node1.Condition.Value);
    }

    [Fact]
    public void RuleInput_maps_nested_filter_groups()
    {
        var rule = new RuleInput(
            "r1",
            Strategy: IdentificationStrategy.AirdateExtraction,
            Filters: new FilterGroupInput(
                Any:
                [
                    new FilterNodeInput(
                        All:
                        [
                            new FilterNodeInput(Field: FilterField.Channel, Op: FilterOp.Eq, Value: "ARD")
                        ])
                ]));

        var result = rule.ToMessage();

        Assert.NotNull(result);
        Assert.NotNull(result.Filters);
        Assert.NotNull(result.Filters.Any);
        var outerNode = Assert.Single(result.Filters.Any);
        var groupNode = Assert.IsType<MsgScoring.FilterNode.GroupNode>(outerNode);
        Assert.NotNull(groupNode.Group.All);
        var innerNode = Assert.Single(groupNode.Group.All);
        var condNode = Assert.IsType<MsgScoring.FilterNode.ConditionNode>(innerNode);
        Assert.Equal(MsgScoring.FilterField.Channel, condNode.Condition.Field);
        Assert.Equal("ARD", condNode.Condition.Value);
    }
}
