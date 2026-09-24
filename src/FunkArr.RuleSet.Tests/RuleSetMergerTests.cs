using FunkArr.RuleSet.DiskModel;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetMergerTests
{
    [Fact]
    public void Resolve_both_null_returns_null()
    {
        Assert.Null(RuleSetMerger.Resolve(null, null));
    }

    [Fact]
    public void Resolve_community_only_returns_community()
    {
        var community = new DiskRuleSet { Topic = "Community" };
        var result = RuleSetMerger.Resolve(community, null);
        Assert.Equal("Community", result!.Topic);
    }

    [Fact]
    public void Resolve_local_only_returns_local()
    {
        var local = new DiskRuleSet { Topic = "Local" };
        var result = RuleSetMerger.Resolve(null, local);
        Assert.Equal("Local", result!.Topic);
    }

    [Fact]
    public void Resolve_standalone_local_ignores_community()
    {
        var community = new DiskRuleSet
        {
            Topic = "Community",
            Rules = [new DiskRule { Id = "comm-rule" }],
        };
        var local = new DiskRuleSet
        {
            Topic = "Local",
            Standalone = true,
            Rules = [new DiskRule { Id = "local-rule" }],
        };

        var result = RuleSetMerger.Resolve(community, local);
        Assert.Equal("Local", result!.Topic);
        Assert.Single(result.Rules!);
        Assert.Equal("local-rule", result.Rules![0].Id);
    }

    [Fact]
    public void Resolve_merge_combines_rules()
    {
        var community = new DiskRuleSet
        {
            Topic = "Show",
            Rules =
            [
                new DiskRule { Id = "comm-1", Priority = 0 },
                new DiskRule { Id = "comm-2", Priority = 1 },
            ],
        };
        var local = new DiskRuleSet
        {
            Topic = "Show",
            Rules = [new DiskRule { Id = "local-3", Priority = 2 }],
        };

        var result = RuleSetMerger.Resolve(community, local);
        Assert.Equal(3, result!.Rules!.Count);
    }

    [Fact]
    public void Resolve_merge_local_rule_replaces_community()
    {
        var community = new DiskRuleSet
        {
            Topic = "Show",
            Rules = [new DiskRule { Id = "shared-rule", Priority = 0, Strategy = "itemTitleExact" }],
        };
        var local = new DiskRuleSet
        {
            Topic = "Show",
            Rules = [new DiskRule { Id = "shared-rule", Priority = 0, Strategy = "itemTitleIncludes" }],
        };

        var result = RuleSetMerger.Resolve(community, local);
        Assert.Single(result!.Rules!);
        Assert.Equal("itemTitleIncludes", result.Rules![0].Strategy);
    }

    [Fact]
    public void Resolve_merge_disables_community_rules()
    {
        var community = new DiskRuleSet
        {
            Topic = "Show",
            Rules =
            [
                new DiskRule { Id = "keep-rule", Priority = 0 },
                new DiskRule { Id = "skip-rule", Priority = 1 },
            ],
        };
        var local = new DiskRuleSet
        {
            Topic = "Show",
            Disable = ["skip-rule"],
            Rules = [],
        };

        var result = RuleSetMerger.Resolve(community, local);
        Assert.Single(result!.Rules!);
        Assert.Equal("keep-rule", result.Rules![0].Id);
    }

    [Fact]
    public void Resolve_merge_combines_aliases()
    {
        var community = new DiskRuleSet { Topic = "Show", Aliases = ["Alias A"], Rules = [] };
        var local = new DiskRuleSet { Topic = "Show", Aliases = ["Alias B"], Rules = [] };

        var result = RuleSetMerger.Resolve(community, local);
        Assert.Equal(2, result!.Aliases!.Count);
    }

    [Fact]
    public void Resolve_merge_local_media_overrides_community()
    {
        var community = new DiskRuleSet
        {
            Topic = "Show",
            Media = new DiskMedia { TvdbId = 100, Name = "Comm" },
            Rules = [],
        };
        var local = new DiskRuleSet
        {
            Topic = "Show",
            Media = new DiskMedia { TvdbId = 200 },
            Rules = [],
        };

        var result = RuleSetMerger.Resolve(community, local);
        Assert.Equal(200, result!.Media!.TvdbId);
        Assert.Equal("Comm", result.Media!.Name);
    }
}
