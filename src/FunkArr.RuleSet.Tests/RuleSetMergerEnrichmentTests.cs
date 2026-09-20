using FunkArr.Messages.Enrichment;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetMergerEnrichmentTests
{
    [Fact]
    public void BuildEnrichmentConfig_Default_When_No_Enrichment_Section()
    {
        var config = RuleSetMerger.BuildEnrichmentConfig(null);

        Assert.True(config.Enabled);
        Assert.Equal([EnrichmentMethod.Title, EnrichmentMethod.Airdate], config.Methods);
        Assert.Equal(0.7f, config.Title.Threshold);
        Assert.Equal(7, config.Airdate.Tolerance);
        Assert.Equal(0.35f, config.Runtime.Tolerance);
        Assert.Equal(RuntimeMode.Tiebreaker, config.Runtime.Mode);
        Assert.Equal(1, config.Year.Tolerance);
    }

    [Fact]
    public void ExtractIdentity_Returns_Default_Enrichment_When_No_Section()
    {
        var json = """{"topic": "Test", "rules": []}""";
        var identity = RuleSetMerger.ExtractIdentity(json, null);

        Assert.NotNull(identity);
        Assert.True(identity.Value.Enrichment.Enabled);
        Assert.Equal([EnrichmentMethod.Title, EnrichmentMethod.Airdate], identity.Value.Enrichment.Methods);
    }

    [Fact]
    public void ExtractIdentity_Parses_Enrichment_Section()
    {
        var json = """
        {
            "topic": "Test",
            "rules": [],
            "enrichment": {
                "methods": ["airdate"],
                "title": { "threshold": 0.5 }
            }
        }
        """;
        var identity = RuleSetMerger.ExtractIdentity(json, null);

        Assert.NotNull(identity);
        Assert.Equal([EnrichmentMethod.Airdate], identity.Value.Enrichment.Methods);
        Assert.Equal(0.5f, identity.Value.Enrichment.Title.Threshold);
        Assert.Equal(7, identity.Value.Enrichment.Airdate.Tolerance);
    }

    [Fact]
    public void ExtractIdentity_Local_Overrides_Community_Enrichment()
    {
        var community = """
        {
            "topic": "Test",
            "rules": [],
            "enrichment": {
                "title": { "threshold": 0.7 }
            }
        }
        """;
        var local = """
        {
            "topic": "Test",
            "rules": [],
            "enrichment": {
                "title": { "threshold": 0.5 }
            }
        }
        """;
        var identity = RuleSetMerger.ExtractIdentity(community, local);

        Assert.NotNull(identity);
        Assert.Equal(0.5f, identity.Value.Enrichment.Title.Threshold);
    }

    [Fact]
    public void ExtractIdentity_Disabled_Enrichment()
    {
        var json = """
        {
            "topic": "Test",
            "rules": [],
            "enrichment": { "enabled": false }
        }
        """;
        var identity = RuleSetMerger.ExtractIdentity(json, null);

        Assert.NotNull(identity);
        Assert.False(identity.Value.Enrichment.Enabled);
    }
}
