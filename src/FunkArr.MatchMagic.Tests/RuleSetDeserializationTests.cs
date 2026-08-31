using System.Text.Json;
using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class RuleSetDeserializationTests
{
    [Fact]
    public void Tatort_ruleset_round_trips()
    {
        var json = TestData.LoadResource("tatort-ruleset.json");

        var ruleSet = RuleSet.FromJson(json);

        Assert.Equal("Tatort", ruleSet.Topic);
        Assert.Equal(2, ruleSet.Aliases!.Count);
        Assert.Contains("Tatort - Münster", ruleSet.Aliases);
        Assert.Equal(0.9f, ruleSet.Confidence);
        Assert.Equal(83214, ruleSet.Media!.TvdbId);
        Assert.Equal("tt0806910", ruleSet.Media.ImdbId);
        Assert.Equal(2116, ruleSet.Media.TmdbId);
        Assert.Equal("Tatort", ruleSet.Media.Name);
        Assert.Equal(MediaType.Show, ruleSet.Media.Type);
    }

    [Fact]
    public void Rule_ids_deserialized()
    {
        var json = TestData.LoadResource("tatort-ruleset.json");
        var ruleSet = RuleSet.FromJson(json);

        Assert.Equal("season-episode", ruleSet.EffectiveRules[0].Id);
        Assert.Equal("title-fallback", ruleSet.EffectiveRules[1].Id);
    }

    [Fact]
    public void Tatort_rules_deserialized_correctly()
    {
        var json = TestData.LoadResource("tatort-ruleset.json");
        var ruleSet = RuleSet.FromJson(json);

        Assert.Equal(2, ruleSet.EffectiveRules.Count);

        var rule0 = ruleSet.EffectiveRules[0];
        Assert.Equal(0, rule0.Priority);
        Assert.Equal(0.95f, rule0.Confidence);
        Assert.Equal(MatchStrategy.SeasonAndEpisodeNumber, rule0.Strategy);
        Assert.NotNull(rule0.SeasonRegex);
        Assert.NotNull(rule0.EpisodeRegex);

        var rule1 = ruleSet.EffectiveRules[1];
        Assert.Equal(10, rule1.Priority);
        Assert.Equal(0.7f, rule1.Confidence);
        Assert.Equal(MatchStrategy.ItemTitleExact, rule1.Strategy);
        Assert.NotNull(rule1.TitleRules);
        Assert.Single(rule1.TitleRules);
    }

    [Fact]
    public void Filter_groups_deserialized_with_all_any_not()
    {
        var json = TestData.LoadResource("tatort-ruleset.json");
        var ruleSet = RuleSet.FromJson(json);

        var filters = ruleSet.EffectiveRules[0].Filters;
        Assert.NotNull(filters.All);
        Assert.Single(filters.All);
        Assert.NotNull(filters.Not);
        Assert.Equal(2, filters.Not.Count);
        Assert.NotNull(filters.Any);
        Assert.Equal(2, filters.Any.Count);
    }

    [Fact]
    public void Round_trip_preserves_structure()
    {
        var json = TestData.LoadResource("tatort-ruleset.json");
        var ruleSet = RuleSet.FromJson(json);
        var reJson = ruleSet.ToJson();
        var ruleSet2 = RuleSet.FromJson(reJson);

        Assert.Equal(ruleSet.Topic, ruleSet2.Topic);
        Assert.Equal(ruleSet.EffectiveRules.Count, ruleSet2.EffectiveRules.Count);
        Assert.Equal(ruleSet.EffectiveRules[0].Strategy, ruleSet2.EffectiveRules[0].Strategy);
        Assert.Equal(ruleSet.EffectiveRules[1].Strategy, ruleSet2.EffectiveRules[1].Strategy);
    }

    [Fact]
    public void Minimal_ruleset_deserializes()
    {
        var json = """{"topic":"Test","rules":[]}""";

        var ruleSet = RuleSet.FromJson(json);

        Assert.Equal("Test", ruleSet.Topic);
        Assert.Empty(ruleSet.EffectiveRules);
        Assert.Null(ruleSet.Aliases);
        Assert.Null(ruleSet.Media);
        Assert.Null(ruleSet.Confidence);
        Assert.False(ruleSet.Standalone);
        Assert.Null(ruleSet.Disable);
    }

    [Fact]
    public void Standalone_and_disable_deserialized()
    {
        var json = """{"topic":"Test","standalone":true,"disable":["some-rule"],"rules":[]}""";

        var ruleSet = RuleSet.FromJson(json);

        Assert.True(ruleSet.Standalone);
        Assert.NotNull(ruleSet.Disable);
        Assert.Single(ruleSet.Disable);
        Assert.Equal("some-rule", ruleSet.Disable[0]);
    }

    [Fact]
    public void MediaType_enum_deserialized()
    {
        var json = """{"topic":"Test","media":{"name":"Film","type":"movie"},"rules":[]}""";

        var ruleSet = RuleSet.FromJson(json);

        Assert.Equal(MediaType.Movie, ruleSet.Media!.Type);
    }

    [Fact]
    public void MediaType_show_is_default()
    {
        var json = """{"topic":"Test","media":{"name":"Show"},"rules":[]}""";

        var ruleSet = RuleSet.FromJson(json);

        Assert.Equal(MediaType.Show, ruleSet.Media!.Type);
    }

    [Fact]
    public void Unknown_media_type_throws()
    {
        var json = """{"topic":"Test","media":{"name":"X","type":"podcast"},"rules":[]}""";

        Assert.Throws<JsonException>(() => RuleSet.FromJson(json));
    }
}
