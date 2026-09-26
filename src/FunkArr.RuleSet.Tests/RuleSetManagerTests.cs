using System.IO.Abstractions;
using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetManagerTests : TestKit
{
    private readonly string _tempDir;
    private readonly TestDataFiles _dataFiles;
    private readonly DataPaths _dataPaths;
    private readonly RuleSetStore _store;

    public RuleSetManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "community"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "local"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "temp"));

        _dataPaths = new DataPaths(
            Options.Create(new FunkArrOptions { DataPath = _tempDir }),
            Options.Create(new DownloadOptions()));
        _dataPaths.EnsureDirectories();
        _dataFiles = new TestDataFiles(new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance));
        _store = new RuleSetStore(_dataFiles, _dataPaths);
    }

    private const string _sampleJson = """
                                       {
                                         "topic": "Test Show",
                                         "aliases": [],
                                         "confidence": 0.9,
                                         "rules": [
                                           {
                                             "id": "airdate",
                                             "priority": 0,
                                             "strategy": "itemTitleEqualsAirdate"
                                           }
                                         ]
                                       }
                                       """;

    [Fact]
    public void Startup_scan_dispatches_LoadRuleSet_per_file()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);

        File.WriteAllText(Path.Combine(_dataPaths.CommunityRuleSets, "show-a.json"), _sampleJson);
        File.WriteAllText(Path.Combine(_dataPaths.CommunityRuleSets, "show-b.json"), _sampleJson);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths)));

        var msg1 = shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();
        var msg2 = shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        var ids = new[] { msg1.RuleSetId, msg2.RuleSetId }.OrderBy(x => x).ToArray();
        Assert.Equal("show-a", ids[0]);
        Assert.Equal("show-b", ids[1]);

        Cleanup();
    }

    [Fact]
    public void Startup_scan_includes_local_path_when_both_exist()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);

        File.WriteAllText(Path.Combine(_dataPaths.CommunityRuleSets, "show-a.json"), _sampleJson);
        File.WriteAllText(Path.Combine(_dataPaths.LocalRuleSets, "show-a.json"), _sampleJson);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths)));

        var msg = shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();
        Assert.Equal("show-a", msg.RuleSetId);
        Assert.NotNull(msg.CommunityPath);
        Assert.NotNull(msg.LocalPath);

        Cleanup();
    }

    [Fact]
    public void FileWatcher_triggers_LoadRuleSet_for_new_file()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths, TimeSpan.FromMilliseconds(50))));

        shardProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
        AwaitCondition(() => _dataFiles.Watchers.Count >= 2);

        var filePath = Path.Combine(_dataPaths.CommunityRuleSets, "new-show.json");
        File.WriteAllText(filePath, _sampleJson);
        _dataFiles.Watchers[0].SimulateCreated(filePath);

        var msg = shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>(TimeSpan.FromSeconds(5));
        Assert.Equal("new-show", msg.RuleSetId);

        Cleanup();
    }

    [Fact]
    public void FileWatcher_triggers_RemoveRuleSet_for_deleted_file()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);

        var filePath = Path.Combine(_dataPaths.CommunityRuleSets, "temp-show.json");
        File.WriteAllText(filePath, _sampleJson);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths, TimeSpan.FromMilliseconds(50))));

        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();
        AwaitCondition(() => _dataFiles.Watchers.Count >= 2);

        File.Delete(filePath);
        _dataFiles.Watchers[0].SimulateDeleted(filePath);

        var msg = shardProbe.ExpectMsg<RuleSetWorker.RemoveRuleSet>(TimeSpan.FromSeconds(5));
        Assert.Equal("temp-show", msg.RuleSetId);

        Cleanup();
    }

    [Fact]
    public void QueryDetail_forwards_to_ShardRegion()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);

        File.WriteAllText(Path.Combine(_dataPaths.CommunityRuleSets, "test-show.json"), _sampleJson);

        var manager = Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths)));
        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        manager.Tell(new QueryRuleSetDetail("test-show"));
        var forwarded = shardProbe.ExpectMsg<QueryRuleSetDetail>();
        Assert.Equal("test-show", forwarded.RuleSetId);

        Cleanup();
    }

    [Fact]
    public void QueryDetail_returns_not_found_when_file_deleted_after_scan()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);

        var filePath = Path.Combine(_dataPaths.CommunityRuleSets, "deleted-show.json");
        File.WriteAllText(filePath, _sampleJson);

        var manager = Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths)));
        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        File.Delete(filePath);

        manager.Tell(new QueryRuleSetDetail("deleted-show"));
        var forwarded = shardProbe.ExpectMsg<QueryRuleSetDetail>();
        Assert.Equal("deleted-show", forwarded.RuleSetId);

        Cleanup();
    }

    [Fact]
    public void QueryRuleSetListWithStats_returns_combined_entries_with_stats()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();
        var historyProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);
        registry.Register<IHistoryRegion>(historyProbe);

        File.WriteAllText(Path.Combine(_dataPaths.CommunityRuleSets, "show-a.json"), _sampleJson);
        File.WriteAllText(Path.Combine(_dataPaths.CommunityRuleSets, "show-b.json"), _sampleJson);

        var manager = Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths)));
        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();
        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        manager.Tell(new WorkerReady("show-a", 1, "community"));
        manager.Tell(new WorkerReady("show-b", 1, "community"));

        manager.Tell(new QueryRuleSetListWithStats());

        var lastRun = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 2; i++)
        {
            var statsQuery = historyProbe.ExpectMsg<QueryScoringStats>();
            var stats = statsQuery.RuleSetId == "show-a"
                ? new ScoringStatsResult(lastRun, 0.85)
                : new ScoringStatsResult(null, null);
            historyProbe.Reply(stats);
        }

        var result = ExpectMsg<RuleSetListWithStatsResult>();
        Assert.Equal(2, result.Entries.Length);

        var entryWithStats = result.Entries.First(e => e.LastRun is not null);
        Assert.Equal(lastRun, entryWithStats.LastRun);
        Assert.Equal(0.85, entryWithStats.MatchRate);

        var entryWithoutStats = result.Entries.First(e => e.LastRun is null);
        Assert.Null(entryWithoutStats.MatchRate);

        Cleanup();
    }

    [Fact]
    public void QueryRuleSetListWithStats_returns_null_stats_on_timeout()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var scoringProbe = CreateTestProbe();
        var historyProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IScoringManager>(scoringProbe);
        registry.Register<IHistoryRegion>(historyProbe);

        File.WriteAllText(Path.Combine(_dataPaths.CommunityRuleSets, "show-a.json"), _sampleJson);

        var manager = Sys.ActorOf(Props.Create(() => new RuleSetManager(_store, _dataFiles, _dataPaths)));
        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        manager.Tell(new WorkerReady("show-a", 1, "community"));

        manager.Tell(new QueryRuleSetListWithStats());

        historyProbe.ExpectMsg<QueryScoringStats>();

        var result = ExpectMsg<RuleSetListWithStatsResult>(TimeSpan.FromSeconds(10));
        Assert.Single(result.Entries);
        Assert.Null(result.Entries[0].LastRun);
        Assert.Null(result.Entries[0].MatchRate);

        Cleanup();
    }

    private void Cleanup()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch
        {
            // ignored
        }
    }
}
