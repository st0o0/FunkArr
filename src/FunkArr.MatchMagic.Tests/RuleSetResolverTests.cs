using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class RuleSetResolverTests
{
    private static readonly MediaRef _media = new(TvdbId: 1, Name: "Test", Type: MediaType.Show);

    private static Rule MakeRule(string id, int priority = 0, float? confidence = null) =>
        new(id, priority, confidence, MatchStrategy.ItemTitleEqualsAirdate, new FilterGroup());

    [Fact]
    public void Both_null_returns_null()
    {
        var result = RuleSetResolver.Resolve(null, null);

        Assert.Null(result);
    }

    [Fact]
    public void Only_community_returns_community()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);

        var result = RuleSetResolver.Resolve(community, null);

        Assert.Same(community, result);
    }

    [Fact]
    public void Only_local_returns_local()
    {
        var local = new RuleSet("Test", Media: _media, Confidence: 0.8f, Rules: [MakeRule("rule-a")]);

        var result = RuleSetResolver.Resolve(null, local);

        Assert.Same(local, result);
    }

    [Fact]
    public void Standalone_local_ignores_community()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a"), MakeRule("rule-b", 1)]);
        var local = new RuleSet("Test", Standalone: true, Media: _media, Confidence: 0.8f, Rules: [MakeRule("rule-c")]);

        var result = RuleSetResolver.Resolve(community, local);

        Assert.Same(local, result);
    }

    [Fact]
    public void Extend_adds_new_rules()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test", Rules: [MakeRule("rule-b", 5)]);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal(2, result.EffectiveRules.Count);
        Assert.Contains(result.EffectiveRules, r => r.Id == "rule-a");
        Assert.Contains(result.EffectiveRules, r => r.Id == "rule-b");
    }

    [Fact]
    public void Extend_replaces_same_id()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a", confidence: 0.5f)]);
        var local = new RuleSet("Test", Rules: [MakeRule("rule-a", confidence: 0.99f)]);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Single(result.EffectiveRules);
        Assert.Equal(0.99f, result.EffectiveRules[0].Confidence);
    }

    [Fact]
    public void Extend_disables_rules()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a"), MakeRule("rule-b", 1)]);
        var local = new RuleSet("Test", Disable: ["rule-b"]);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Single(result.EffectiveRules);
        Assert.Equal("rule-a", result.EffectiveRules[0].Id);
    }

    [Fact]
    public void Extend_combined_replace_add_disable()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f,
            Rules: [MakeRule("rule-a"), MakeRule("rule-b", 1), MakeRule("rule-c", 2)]);
        var local = new RuleSet("Test",
            Disable: ["rule-c"],
            Rules: [MakeRule("rule-a", confidence: 0.99f), MakeRule("rule-d", 5)]);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal(3, result.EffectiveRules.Count);
        Assert.Contains(result.EffectiveRules, r => r.Id == "rule-a" && r.Confidence == 0.99f);
        Assert.Contains(result.EffectiveRules, r => r.Id == "rule-b");
        Assert.Contains(result.EffectiveRules, r => r.Id == "rule-d");
        Assert.DoesNotContain(result.EffectiveRules, r => r.Id == "rule-c");
    }

    [Fact]
    public void Merged_rules_sorted_by_priority()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a", 10)]);
        var local = new RuleSet("Test", Rules: [MakeRule("rule-b", 5)]);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal("rule-b", result.EffectiveRules[0].Id);
        Assert.Equal("rule-a", result.EffectiveRules[1].Id);
    }

    [Fact]
    public void Aliases_union_merged()
    {
        var community = new RuleSet("Test", Aliases: ["A", "B"], Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test", Aliases: ["B", "C"]);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal(3, result.Aliases!.Count);
        Assert.Contains("A", result.Aliases);
        Assert.Contains("B", result.Aliases);
        Assert.Contains("C", result.Aliases);
    }

    [Fact]
    public void Local_no_aliases_inherits_community()
    {
        var community = new RuleSet("Test", Aliases: ["A"], Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test");

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Single(result.Aliases!);
        Assert.Equal("A", result.Aliases[0]);
    }

    [Fact]
    public void Confidence_local_wins()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test", Confidence: 0.7f);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal(0.7f, result.Confidence);
    }

    [Fact]
    public void Confidence_inherits_when_local_null()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test");

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal(0.9f, result.Confidence);
    }

    [Fact]
    public void Media_local_wins()
    {
        var communityMedia = new MediaRef(TvdbId: 1, Name: "Community");
        var localMedia = new MediaRef(TvdbId: 99, Name: "Local");
        var community = new RuleSet("Test", Media: communityMedia, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test", Media: localMedia);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal(99, result.Media!.TvdbId);
    }

    [Fact]
    public void Media_inherits_when_local_null()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test");

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal(1, result.Media!.TvdbId);
    }

    [Fact]
    public void Topic_uses_community_canonical()
    {
        var community = new RuleSet("Tatort", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("tatort");

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Equal("Tatort", result.Topic);
    }

    [Fact]
    public void Disable_unknown_id_silent()
    {
        var community = new RuleSet("Test", Media: _media, Confidence: 0.9f, Rules: [MakeRule("rule-a")]);
        var local = new RuleSet("Test", Disable: ["nonexistent"]);

        var result = RuleSetResolver.Resolve(community, local)!;

        Assert.Single(result.EffectiveRules);
        Assert.Equal("rule-a", result.EffectiveRules[0].Id);
    }
}
