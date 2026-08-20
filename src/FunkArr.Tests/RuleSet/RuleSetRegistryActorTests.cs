using System.Text.Json;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.RuleSet;

public sealed class RuleSetRegistryActorTests : Akka.Hosting.TestKit.TestKit
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "funkarr-tests", Guid.NewGuid().ToString("N"));

    public RuleSetRegistryActorTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton(Options.Create(new FunkArrOptions
        {
            RuleSetPath = _tempDir,
            RuleSetSourceUrl = "http://localhost:1/nonexistent",
            RuleSetRefreshMode = "legacy-url",
            RuleSetRefreshIntervalMinutes = 999,
        }));
        services.AddHttpClient();
        services.AddSingleton<GitHubReleaseClient>();
        services.AddHttpClient("GitHubRelease");
        services.AddHttpClient<MediathekClient>(client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/");
        });
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    protected override async Task AfterAllAsync()
    {
        await base.AfterAllAsync();
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    private IActorRef CreateActor()
    {
        var resolver = DependencyResolver.For(Sys);
        var props = resolver.Props<RuleSetRegistryActor>();
        return Sys.ActorOf(props);
    }

    private void WriteCommunityRuleSet(RuleSetFile ruleSet) =>
        WriteRuleSetToLayer("community", ruleSet);

    private void WriteLocalRuleSet(RuleSetFile ruleSet) =>
        WriteRuleSetToLayer("local", ruleSet);

    private void WriteRuleSetToLayer(string layer, RuleSetFile ruleSet)
    {
        var dir = Path.Combine(_tempDir, layer);
        Directory.CreateDirectory(dir);
        var slug = TopicSlugGenerator.Generate(ruleSet.Topic);
        var path = Path.Combine(dir, $"{slug}.json");
        var json = JsonSerializer.Serialize(ruleSet, RuleSetJsonOptions.Default);
        File.WriteAllText(path, json);
    }

    private void ClearAllLayers()
    {
        foreach (var layer in new[] { "community", "generated", "local" })
        {
            var dir = Path.Combine(_tempDir, layer);
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
                    File.Delete(file);
            }
        }
    }

    private static RuleSetFile CreateRuleSet(
        string topic,
        IReadOnlyList<string>? aliases = null,
        int? tvdbId = null,
        IReadOnlyList<Rule>? rules = null,
        string source = "community",
        OverrideConfig? overrides = null) =>
        new()
        {
            Topic = topic,
            Aliases = aliases ?? [],
            Media = new MediaReference { Name = topic, TvdbId = tvdbId },
            Source = source,
            Rules = rules ?? [new Rule { Priority = 0, Strategy = MatchingStrategy.ItemTitleIncludes }],
            Overrides = overrides,
        };

    private static Rule MakeRule(int priority, MatchingStrategy strategy = MatchingStrategy.ItemTitleIncludes) =>
        new() { Priority = priority, Strategy = strategy };

    private static async Task<RuleSetRegistryActor.RulesResponse> Ask(IActorRef actor, string topic, int? tvdbId = null)
    {
        var msg = new RuleSetRegistryActor.GetRulesForTopic(topic, tvdbId);
        return await actor.Ask<RuleSetRegistryActor.RulesResponse>(msg, TimeSpan.FromSeconds(5));
    }

    [Fact(Timeout = 5000)]
    public async Task AliasLookup_ReturnsRulesWhenQueriedByAlias()
    {
        ClearAllLayers();
        var rule = MakeRule(10, MatchingStrategy.SeasonAndEpisodeNumber);
        WriteCommunityRuleSet(CreateRuleSet(
            "Tatort",
            aliases: ["tatort", "Tatort Schimanski"],
            rules: [rule]));

        var actor = CreateActor();
        var response = await Ask(actor, "Tatort Schimanski");

        Assert.Single(response.Rules);
        Assert.Equal(MatchingStrategy.SeasonAndEpisodeNumber, response.Rules[0].Strategy);
        Assert.Equal(10, response.Rules[0].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task AliasLookup_CaseInsensitive()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "ZDF Magazin Royale",
            aliases: ["zdf magazin royale"],
            rules: [MakeRule(5)]));

        var actor = CreateActor();
        var response = await Ask(actor, "ZDF MAGAZIN ROYALE");

        Assert.Single(response.Rules);
    }

    [Fact(Timeout = 5000)]
    public async Task AliasLookup_TopicLookupTakesPrecedenceOverAlias()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "Show A",
            aliases: ["Show B"],
            rules: [MakeRule(1)]));

        WriteCommunityRuleSet(CreateRuleSet(
            "Show B",
            rules: [MakeRule(99)]));

        var actor = CreateActor();
        var response = await Ask(actor, "Show B");

        Assert.Single(response.Rules);
        Assert.Equal(99, response.Rules[0].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task AliasLookup_ReturnsEmptyWhenNoMatch()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "heute-show",
            aliases: ["heuteshow"],
            rules: [MakeRule(1)]));

        var actor = CreateActor();
        var response = await Ask(actor, "nonexistent topic");

        Assert.Empty(response.Rules);
    }

    [Fact(Timeout = 5000)]
    public async Task MergeOverride_CombinesBaseAndLocalRules()
    {
        ClearAllLayers();
        var baseRule = MakeRule(10, MatchingStrategy.SeasonAndEpisodeNumber);
        WriteCommunityRuleSet(CreateRuleSet(
            "Feuer & Flamme",
            rules: [baseRule]));

        var addedRule = MakeRule(20, MatchingStrategy.ItemTitleExact);
        WriteLocalRuleSet(CreateRuleSet(
            "Feuer & Flamme",
            source: "local",
            overrides: new OverrideConfig
            {
                Mode = OverrideMode.Merge,
                Add = [addedRule],
            }));

        var actor = CreateActor();
        var response = await Ask(actor, "Feuer & Flamme");

        Assert.Equal(2, response.Rules.Count);
        Assert.Equal(10, response.Rules[0].Priority);
        Assert.Equal(MatchingStrategy.SeasonAndEpisodeNumber, response.Rules[0].Strategy);
        Assert.Equal(20, response.Rules[1].Priority);
        Assert.Equal(MatchingStrategy.ItemTitleExact, response.Rules[1].Strategy);
    }

    [Fact(Timeout = 5000)]
    public async Task MergeOverride_RemovesRuleByIndex()
    {
        ClearAllLayers();
        var ruleA = MakeRule(10, MatchingStrategy.SeasonAndEpisodeNumber);
        var ruleB = MakeRule(20, MatchingStrategy.ItemTitleIncludes);
        var ruleC = MakeRule(30, MatchingStrategy.ItemTitleExact);
        WriteCommunityRuleSet(CreateRuleSet(
            "Terra X",
            rules: [ruleA, ruleB, ruleC]));

        WriteLocalRuleSet(CreateRuleSet(
            "Terra X",
            source: "local",
            overrides: new OverrideConfig
            {
                Mode = OverrideMode.Merge,
                Remove = [1],
            }));

        var actor = CreateActor();
        var response = await Ask(actor, "Terra X");

        Assert.Equal(2, response.Rules.Count);
        Assert.Equal(10, response.Rules[0].Priority);
        Assert.Equal(30, response.Rules[1].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task MergeOverride_RemoveAndAddCombined()
    {
        ClearAllLayers();
        var ruleA = MakeRule(10, MatchingStrategy.SeasonAndEpisodeNumber);
        var ruleB = MakeRule(20, MatchingStrategy.ItemTitleIncludes);
        WriteCommunityRuleSet(CreateRuleSet(
            "Sturm der Liebe",
            rules: [ruleA, ruleB]));

        var replacement = MakeRule(15, MatchingStrategy.ByAbsoluteEpisodeNumber);
        WriteLocalRuleSet(CreateRuleSet(
            "Sturm der Liebe",
            source: "local",
            overrides: new OverrideConfig
            {
                Mode = OverrideMode.Merge,
                Remove = [1],
                Add = [replacement],
            }));

        var actor = CreateActor();
        var response = await Ask(actor, "Sturm der Liebe");

        Assert.Equal(2, response.Rules.Count);
        Assert.Equal(10, response.Rules[0].Priority);
        Assert.Equal(15, response.Rules[1].Priority);
        Assert.Equal(MatchingStrategy.ByAbsoluteEpisodeNumber, response.Rules[1].Strategy);
    }

    [Fact(Timeout = 5000)]
    public async Task AliasConflict_LaterFileWins()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "Tatort",
            aliases: ["krimi"],
            rules: [MakeRule(1)]));

        WriteCommunityRuleSet(CreateRuleSet(
            "Polizeiruf 110",
            aliases: ["krimi"],
            rules: [MakeRule(99)]));

        var actor = CreateActor();
        var response = await Ask(actor, "krimi");

        Assert.Single(response.Rules);
        Assert.True(
            response.Rules[0].Priority == 1 || response.Rules[0].Priority == 99,
            "Alias should resolve to exactly one of the conflicting rulesets");
    }

    [Fact(Timeout = 5000)]
    public async Task ReplaceOverride_LocalReplacesCommunitySameTopic()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "heute-show",
            aliases: ["heuteshow"],
            rules: [MakeRule(10, MatchingStrategy.SeasonAndEpisodeNumber)]));

        WriteLocalRuleSet(CreateRuleSet(
            "heute-show",
            aliases: ["heuteshow", "heute show"],
            source: "local",
            rules: [MakeRule(50, MatchingStrategy.ItemTitleExact)]));

        var actor = CreateActor();
        var response = await Ask(actor, "heute-show");

        Assert.Single(response.Rules);
        Assert.Equal(50, response.Rules[0].Priority);
        Assert.Equal(MatchingStrategy.ItemTitleExact, response.Rules[0].Strategy);
    }

    [Fact(Timeout = 5000)]
    public async Task ReplaceOverride_LocalAliasesReplaceCommunitys()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "heute-show",
            aliases: ["heuteshow"],
            rules: [MakeRule(10)]));

        WriteLocalRuleSet(CreateRuleSet(
            "heute-show",
            aliases: ["neue-alias"],
            source: "local",
            rules: [MakeRule(50)]));

        var actor = CreateActor();

        var byNew = await Ask(actor, "neue-alias");
        Assert.Single(byNew.Rules);
        Assert.Equal(50, byNew.Rules[0].Priority);

        var byOldAlias = await Ask(actor, "heuteshow");
        Assert.Single(byOldAlias.Rules);
        Assert.Equal(10, byOldAlias.Rules[0].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task TvdbIdLookup_FindsRulesByTvdbId()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "Die Sendung mit der Maus",
            tvdbId: 12345,
            rules: [MakeRule(5, MatchingStrategy.ItemTitleEqualsAirdate)]));

        var actor = CreateActor();
        var response = await Ask(actor, "unknown-topic", tvdbId: 12345);

        Assert.Single(response.Rules);
        Assert.Equal(MatchingStrategy.ItemTitleEqualsAirdate, response.Rules[0].Strategy);
    }

    [Fact(Timeout = 5000)]
    public async Task MergeOverride_ResultHasLocalSource()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "Checker Tobi",
            rules: [MakeRule(10)]));

        WriteLocalRuleSet(CreateRuleSet(
            "Checker Tobi",
            source: "local",
            overrides: new OverrideConfig
            {
                Mode = OverrideMode.Merge,
                Add = [MakeRule(20)],
            }));

        var actor = CreateActor();
        var response = await Ask(actor, "Checker Tobi");
        Assert.Equal(2, response.Rules.Count);
    }

    [Fact(Timeout = 5000)]
    public async Task MultipleAliases_AllResolveToSameRules()
    {
        ClearAllLayers();
        var rule = MakeRule(42, MatchingStrategy.SeasonAndEpisodeNumber);
        WriteCommunityRuleSet(CreateRuleSet(
            "Tagesschau",
            aliases: ["tagesschau", "ARD Tagesschau", "Tagesschau 20 Uhr"],
            rules: [rule]));

        var actor = CreateActor();

        var r1 = await Ask(actor, "tagesschau");
        var r2 = await Ask(actor, "ARD Tagesschau");
        var r3 = await Ask(actor, "Tagesschau 20 Uhr");

        Assert.Single(r1.Rules);
        Assert.Single(r2.Rules);
        Assert.Single(r3.Rules);
        Assert.Equal(42, r1.Rules[0].Priority);
        Assert.Equal(42, r2.Rules[0].Priority);
        Assert.Equal(42, r3.Rules[0].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task GetAllRulesets_ReturnsAllLoadedTopics()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet("Tatort", rules: [MakeRule(1)]));
        WriteCommunityRuleSet(CreateRuleSet("heute-show", rules: [MakeRule(2)]));

        var actor = CreateActor();
        var response = await actor.Ask<RuleSetRegistryActor.AllRulesetsResponse>(
            new RuleSetRegistryActor.GetAllRulesets(), TimeSpan.FromSeconds(5));

        Assert.Equal(2, response.Rulesets.Count);
        Assert.Contains(response.Rulesets, r => r.Topic == "Tatort");
        Assert.Contains(response.Rulesets, r => r.Topic == "heute-show");
    }

    [Fact(Timeout = 5000)]
    public async Task GetAllRulesets_SummaryContainsCorrectFields()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "Terra X",
            aliases: ["terrax"],
            tvdbId: 42,
            rules: [MakeRule(1), MakeRule(2)],
            source: "community"));

        var actor = CreateActor();
        var response = await actor.Ask<RuleSetRegistryActor.AllRulesetsResponse>(
            new RuleSetRegistryActor.GetAllRulesets(), TimeSpan.FromSeconds(5));

        var summary = Assert.Single(response.Rulesets);
        Assert.Equal("Terra X", summary.Topic);
        Assert.Equal("community", summary.Source);
        Assert.Equal(2, summary.RuleCount);
        Assert.Equal(42, summary.Media.TvdbId);
        Assert.Contains("terrax", summary.Aliases);
    }

    [Fact(Timeout = 5000)]
    public async Task GetRuleSet_ReturnsMatchingRuleset()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet("Tatort", rules: [MakeRule(10)]));

        var actor = CreateActor();
        var response = await actor.Ask<RuleSetRegistryActor.RuleSetResponse>(
            new RuleSetRegistryActor.GetRuleSet("Tatort"), TimeSpan.FromSeconds(5));

        Assert.NotNull(response.RuleSet);
        Assert.Equal("Tatort", response.RuleSet.Topic);
    }

    [Fact(Timeout = 5000)]
    public async Task GetRuleSet_ReturnsNull_WhenTopicNotFound()
    {
        ClearAllLayers();

        var actor = CreateActor();
        var response = await actor.Ask<RuleSetRegistryActor.RuleSetResponse>(
            new RuleSetRegistryActor.GetRuleSet("nonexistent"), TimeSpan.FromSeconds(5));

        Assert.Null(response.RuleSet);
    }

    [Fact(Timeout = 5000)]
    public async Task SaveLocalRuleSet_WritesFileAndMakesItQueryable()
    {
        ClearAllLayers();

        var actor = CreateActor();
        var ruleSet = CreateRuleSet("My Local Show", source: "local", rules: [MakeRule(55)]);

        var saveResponse = await actor.Ask<RuleSetRegistryActor.SaveLocalRuleSetResponse>(
            new RuleSetRegistryActor.SaveLocalRuleSet(ruleSet), TimeSpan.FromSeconds(5));
        Assert.True(saveResponse.Success);

        var getResponse = await actor.Ask<RuleSetRegistryActor.RuleSetResponse>(
            new RuleSetRegistryActor.GetRuleSet("My Local Show"), TimeSpan.FromSeconds(5));
        Assert.NotNull(getResponse.RuleSet);
        Assert.Single(getResponse.RuleSet.Rules);
        Assert.Equal(55, getResponse.RuleSet.Rules[0].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task DeleteLocalRuleSet_RemovesFileAndFallsBackToCommunity()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet("Tatort", rules: [MakeRule(10)]));
        WriteLocalRuleSet(CreateRuleSet("Tatort", source: "local", rules: [MakeRule(99)]));

        var actor = CreateActor();

        // Verify local override is active
        var before = await Ask(actor, "Tatort");
        Assert.Single(before.Rules);
        Assert.Equal(99, before.Rules[0].Priority);

        // Delete local override
        var deleteResponse = await actor.Ask<RuleSetRegistryActor.DeleteLocalRuleSetResponse>(
            new RuleSetRegistryActor.DeleteLocalRuleSet("Tatort"), TimeSpan.FromSeconds(5));
        Assert.True(deleteResponse.Found);

        // Should fall back to community
        var after = await Ask(actor, "Tatort");
        Assert.Single(after.Rules);
        Assert.Equal(10, after.Rules[0].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task DeleteLocalRuleSet_ReturnsFalse_WhenTopicNotFound()
    {
        ClearAllLayers();

        var actor = CreateActor();
        var response = await actor.Ask<RuleSetRegistryActor.DeleteLocalRuleSetResponse>(
            new RuleSetRegistryActor.DeleteLocalRuleSet("nonexistent"), TimeSpan.FromSeconds(5));

        Assert.False(response.Found);
    }

    [Fact(Timeout = 5000)]
    public async Task MergeOverride_OutOfBoundsRemoveIndexIgnored()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "Bares fuer Rares",
            rules: [MakeRule(10)]));

        WriteLocalRuleSet(CreateRuleSet(
            "Bares fuer Rares",
            source: "local",
            overrides: new OverrideConfig
            {
                Mode = OverrideMode.Merge,
                Remove = [99],
                Add = [MakeRule(20)],
            }));

        var actor = CreateActor();
        var response = await Ask(actor, "Bares fuer Rares");

        Assert.Equal(2, response.Rules.Count);
        Assert.Equal(10, response.Rules[0].Priority);
        Assert.Equal(20, response.Rules[1].Priority);
    }

    [Fact(Timeout = 5000)]
    public async Task ReloadLocal_PicksUpNewFiles()
    {
        ClearAllLayers();
        WriteCommunityRuleSet(CreateRuleSet(
            "Wer weiss denn sowas",
            rules: [MakeRule(10)]));

        var actor = CreateActor();

        var before = await Ask(actor, "Lindenstrasse");
        Assert.Empty(before.Rules);

        WriteCommunityRuleSet(CreateRuleSet(
            "Lindenstrasse",
            rules: [MakeRule(77)]));

        actor.Tell(new RuleSetRegistryActor.ReloadLocal());
        await Task.Delay(200);

        var after = await Ask(actor, "Lindenstrasse");
        Assert.Single(after.Rules);
        Assert.Equal(77, after.Rules[0].Priority);
    }
}
