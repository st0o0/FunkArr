using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.RuleSet.DiskModel;

namespace FunkArr.RuleSet.Tests;

public sealed class DiskToMatchingExtensionsTests
{
    [Fact]
    public void ToMatchingConfig_transforms_rules()
    {
        var disk = new DiskRuleSet
        {
            Topic = "Test",
            Confidence = 0.9f,
            Rules =
            [
                new DiskRule { Id = "rule-1", Priority = 0, Strategy = "itemTitleIncludes" },
                new DiskRule { Id = "rule-2", Priority = 1, Strategy = "seasonAndEpisodeNumber" },
            ],
        };

        var config = disk.ToMatchingConfig("test-id");
        Assert.NotNull(config);
        Assert.Equal("test-id", config!.RuleSetId);
        Assert.Equal(0.9f, config.DefaultConfidence);
        Assert.Equal(2, config.Rules.Length);
        Assert.Equal(IdentificationStrategy.TitleIncludes, config.Rules[0].Identification.Strategy);
        Assert.Equal(IdentificationStrategy.SeasonAndEpisodeNumber, config.Rules[1].Identification.Strategy);
    }

    [Fact]
    public void ToMatchingConfig_skips_unknown_strategy()
    {
        var disk = new DiskRuleSet
        {
            Topic = "Test",
            Rules = [new DiskRule { Id = "bad-rule", Strategy = "unknownStrategy" }],
        };

        var config = disk.ToMatchingConfig("test");
        Assert.NotNull(config);
        Assert.Empty(config!.Rules);
    }

    [Fact]
    public void ToIdentity_extracts_all_fields()
    {
        var disk = new DiskRuleSet
        {
            Topic = "Tatort",
            Aliases = ["Tatort AT"],
            Media = new DiskMedia
            {
                Name = "Tatort",
                Type = Messages.MediaType.Show,
                TvdbId = 83214,
                ImdbId = "tt0806910",
            },
            Enrichment = new DiskEnrichment
            {
                Enabled = true,
                Methods = [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
            },
        };

        var identity = disk.ToIdentity();
        Assert.NotNull(identity);
        Assert.Equal("Tatort", identity!.Value.Topic);
        Assert.Equal(["Tatort AT"], identity.Value.Aliases);
        Assert.Equal(83214, identity.Value.TvdbId);
        Assert.Equal("tt0806910", identity.Value.ImdbId);
        Assert.Equal(Messages.MediaType.Show, identity.Value.MediaType);
        Assert.True(identity.Value.Enrichment.Enabled);
    }

    [Fact]
    public void ToEnrichmentConfig_uses_defaults_for_null()
    {
        var config = ((DiskEnrichment?)null).ToEnrichmentConfig();
        Assert.True(config.Enabled);
        Assert.Equal(2, config.Methods.Length);
        Assert.Equal(0.7f, config.Title.Threshold);
        Assert.Equal(7, config.Airdate.Tolerance);
    }

    [Fact]
    public void ToDetailRules_converts_filters_and_titleRules()
    {
        var disk = new DiskRuleSet
        {
            Topic = "Test",
            Rules =
            [
                new DiskRule
                {
                    Id = "rule-with-filters",
                    Strategy = "itemTitleIncludes",
                    TitleRules = [new DiskTitleRule { Type = TitlePartType.Static, Value = " - " }],
                },
            ],
        };

        var rules = disk.ToDetailRules();
        Assert.Single(rules);
        Assert.Equal("rule-with-filters", rules[0].Id);
        Assert.Single(rules[0].TitleRules!);
        Assert.Equal(TitlePartType.Static, rules[0].TitleRules![0].Type);
    }
}
