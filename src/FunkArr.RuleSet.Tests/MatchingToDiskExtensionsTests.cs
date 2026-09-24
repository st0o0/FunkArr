using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.RuleSet.DiskModel;

namespace FunkArr.RuleSet.Tests;

public sealed class MatchingToDiskExtensionsTests
{
    [Fact]
    public void ToDiskRuleSet_converts_strategy_to_disk_string()
    {
        var body = new RuleSetBody(
            "Test", null,
            new RuleSetMediaInput("Test", MediaType.Show),
            0.9f,
            [new RuleSetRuleInput(Id: "rule-1", Strategy: IdentificationStrategy.TitleIncludes)],
            null, null, null);

        var disk = body.ToDiskRuleSet();
        Assert.Equal("itemTitleIncludes", disk.Rules![0].Strategy);
    }

    [Fact]
    public void ToDiskRuleSet_converts_media_type()
    {
        var body = new RuleSetBody(
            "Movie", null,
            new RuleSetMediaInput("Movie", MediaType.Movie),
            null,
            [new RuleSetRuleInput(Id: "rule-1", Strategy: IdentificationStrategy.TitleExact)],
            null, null, null);

        var disk = body.ToDiskRuleSet();
        Assert.Equal(MediaType.Movie, disk.Media!.Type);
    }

    [Fact]
    public void ToDiskRuleSet_converts_all_strategy_types()
    {
        var strategies = new[]
        {
            (IdentificationStrategy.SeasonAndEpisodeNumber, "seasonAndEpisodeNumber"),
            (IdentificationStrategy.AbsoluteEpisodeNumber, "byAbsoluteEpisodeNumber"),
            (IdentificationStrategy.TitleExact, "itemTitleExact"),
            (IdentificationStrategy.TitleIncludes, "itemTitleIncludes"),
            (IdentificationStrategy.AirdateExtraction, "itemTitleEqualsAirdate"),
        };

        foreach (var (strategy, expected) in strategies)
        {
            var body = new RuleSetBody("T", null, new RuleSetMediaInput("T", MediaType.Show), null,
                [new RuleSetRuleInput(Id: "rule-1", Strategy: strategy)], null, null, null);
            Assert.Equal(expected, body.ToDiskRuleSet().Rules![0].Strategy);
        }
    }

    [Fact]
    public void ToDiskRuleSet_converts_filters()
    {
        var body = new RuleSetBody("T", null, new RuleSetMediaInput("T", MediaType.Show), null,
            [new RuleSetRuleInput(
                Id: "rule-1",
                Strategy: IdentificationStrategy.TitleIncludes,
                Filters: new RuleSetFilterGroupInput(
                    All: [new RuleSetFilterConditionInput(FilterField.Duration, FilterOp.GreaterThan, "35")],
                    Not: [new RuleSetFilterConditionInput(FilterField.Title, FilterOp.Contains, "AD")]))],
            null, null, null);

        var disk = body.ToDiskRuleSet();
        Assert.NotNull(disk.Rules![0].Filters);
        Assert.Single(disk.Rules[0].Filters!.All!);
        Assert.Single(disk.Rules[0].Filters!.Not!);
    }

    [Fact]
    public void ToDiskRuleSet_converts_enrichment()
    {
        var body = new RuleSetBody("T", null, new RuleSetMediaInput("T", MediaType.Show), null,
            [new RuleSetRuleInput(Id: "rule-1", Strategy: IdentificationStrategy.TitleIncludes)],
            null, null,
            new EnrichmentConfig(true, [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
                new TitleMatchConfig(0.7f), new AirdateMatchConfig(7),
                new RuntimeMatchConfig(0.35f, RuntimeMode.Tiebreaker), new YearMatchConfig(1)));

        var disk = body.ToDiskRuleSet();
        Assert.NotNull(disk.Enrichment);
        Assert.True(disk.Enrichment!.Enabled);
        Assert.Equal(2, disk.Enrichment.Methods!.Count);
        Assert.Equal(RuntimeMode.Tiebreaker, disk.Enrichment.Runtime!.Mode);
    }

    [Fact]
    public void ToDiskRuleSet_converts_title_rules()
    {
        var body = new RuleSetBody("T", null, new RuleSetMediaInput("T", MediaType.Show), null,
            [new RuleSetRuleInput(
                Id: "rule-1",
                Strategy: IdentificationStrategy.TitleIncludes,
                TitleRules:
                [
                    new RuleSetTitleRuleInput(TitlePartType.Static, Value: " - "),
                    new RuleSetTitleRuleInput(TitlePartType.Regex, FilterField.Title, Pattern: @"\S.*"),
                ])],
            null, null, null);

        var disk = body.ToDiskRuleSet();
        Assert.Equal(2, disk.Rules![0].TitleRules!.Count);
        Assert.Equal(TitlePartType.Static, disk.Rules[0].TitleRules![0].Type);
        Assert.Equal(TitlePartType.Regex, disk.Rules[0].TitleRules![1].Type);
        Assert.Equal(FilterField.Title, disk.Rules[0].TitleRules![1].Field);
    }
}
