using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.RuleSet;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetManagerTests : TestKit
{
    private readonly string _tempDir;
    private readonly IOptionsMonitor<FunkArrOptions> _optionsMonitor;

    public RuleSetManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "community", "rulesets"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "local", "rulesets"));

        var options = new FunkArrOptions { DataPath = _tempDir };
        _optionsMonitor = new TestOptionsMonitor<FunkArrOptions>(options);
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
        var matchMagicProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);

        File.WriteAllText(Path.Combine(_tempDir, "community", "rulesets", "show-a.json"), _sampleJson);
        File.WriteAllText(Path.Combine(_tempDir, "community", "rulesets", "show-b.json"), _sampleJson);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_optionsMonitor)));

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
        var matchMagicProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);

        File.WriteAllText(Path.Combine(_tempDir, "community", "rulesets", "show-a.json"), _sampleJson);
        File.WriteAllText(Path.Combine(_tempDir, "local", "rulesets", "show-a.json"), _sampleJson);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_optionsMonitor)));

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
        var matchMagicProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_optionsMonitor)));

        shardProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(200));

        File.WriteAllText(Path.Combine(_tempDir, "community", "rulesets", "new-show.json"), _sampleJson);

        var msg = shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>(TimeSpan.FromSeconds(3));
        Assert.Equal("new-show", msg.RuleSetId);

        Cleanup();
    }

    [Fact]
    public void FileWatcher_triggers_RemoveRuleSet_for_deleted_file()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);

        var filePath = Path.Combine(_tempDir, "community", "rulesets", "temp-show.json");
        File.WriteAllText(filePath, _sampleJson);

        Sys.ActorOf(Props.Create(() => new RuleSetManager(_optionsMonitor)));

        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        File.Delete(filePath);

        var msg = shardProbe.ExpectMsg<RuleSetWorker.RemoveRuleSet>(TimeSpan.FromSeconds(5));
        Assert.Equal("temp-show", msg.RuleSetId);

        Cleanup();
    }

    [Fact]
    public void QueryDetail_returns_detail_for_known_ruleset()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);

        File.WriteAllText(Path.Combine(_tempDir, "community", "rulesets", "test-show.json"), _sampleJson);

        var manager = Sys.ActorOf(Props.Create(() => new RuleSetManager(_optionsMonitor)));
        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        manager.Tell(new QueryRuleSetDetail("test-show"));
        var result = ExpectMsg<RuleSetDetailResult>();

        Assert.Equal("test-show", result.RuleSetId);
        Assert.Equal("Test Show", result.Identity.Topic);
        Assert.Equal(0.9f, result.DefaultConfidence);
        Assert.Single(result.Rules);
        Assert.Equal("airdate", result.Rules[0].Id);
        Assert.Equal("AirdateExtraction", result.Rules[0].Strategy);
        Assert.NotNull(result.Source.CommunityPath);
        Assert.Null(result.Source.LocalPath);

        Cleanup();
    }

    [Fact]
    public void QueryDetail_returns_not_found_for_unknown_ruleset()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);

        var manager = Sys.ActorOf(Props.Create(() => new RuleSetManager(_optionsMonitor)));

        manager.Tell(new QueryRuleSetDetail("nonexistent"));
        var result = ExpectMsg<RuleSetNotFound>();

        Assert.Equal("nonexistent", result.TopicOrAlias);

        Cleanup();
    }

    [Fact]
    public void QueryDetail_returns_not_found_when_file_deleted_after_scan()
    {
        var shardProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IRuleSetRegion>(shardProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);

        var filePath = Path.Combine(_tempDir, "community", "rulesets", "deleted-show.json");
        File.WriteAllText(filePath, _sampleJson);

        var manager = Sys.ActorOf(Props.Create(() => new RuleSetManager(_optionsMonitor)));
        shardProbe.ExpectMsg<RuleSetWorker.LoadRuleSet>();

        File.Delete(filePath);

        manager.Tell(new QueryRuleSetDetail("deleted-show"));
        var result = ExpectMsg<RuleSetNotFound>();

        Assert.Equal("deleted-show", result.TopicOrAlias);

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
