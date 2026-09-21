using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;

namespace FunkArr.History.Tests;

public sealed class StatsCollectorTests : TestKit
{
    private readonly TestProbe _historyRegionProbe;
    private readonly TestProbe _ruleSetResolverProbe;

    public StatsCollectorTests()
    {
        _historyRegionProbe = CreateTestProbe();
        _ruleSetResolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IHistoryRegion>(_historyRegionProbe);
        registry.Register<IRuleSetResolver>(_ruleSetResolverProbe);
    }

    private IActorRef CreateCollector()
    {
        var actor = Sys.ActorOf(Props.Create<StatsCollector>());
        HandlePreStartBackfill();
        return actor;
    }

    private void HandlePreStartBackfill()
    {
        var query = _ruleSetResolverProbe.ExpectMsg<QueryRegisteredRuleSets>();
        _ruleSetResolverProbe.Reply(new RegisteredRuleSetsResult([]));
    }

    [Fact]
    public void UpdateStats_ThenQueryAllStats_ReturnsUpdatedStats()
    {
        var collector = CreateCollector();
        var stats = new ScoringStatsResult(DateTimeOffset.UtcNow, 0.5, 0.3, 10);

        collector.Tell(new UpdateStats("rs-1", stats));
        collector.Tell(new QueryAllStats(), TestActor);

        var result = ExpectMsg<AllStatsResult>();
        Assert.Single(result.Entries);
        Assert.Equal(stats, result.Entries["rs-1"]);
    }

    [Fact]
    public void RemoveStats_RemovesEntry()
    {
        var collector = CreateCollector();
        var stats = new ScoringStatsResult(DateTimeOffset.UtcNow, 0.5, 0.3, 10);

        collector.Tell(new UpdateStats("rs-1", stats));
        collector.Tell(new RemoveStats("rs-1"));
        collector.Tell(new QueryAllStats(), TestActor);

        var result = ExpectMsg<AllStatsResult>();
        Assert.Empty(result.Entries);
    }

    [Fact]
    public void QueryAllStats_EmptyState_ReturnsEmptyEntries()
    {
        var collector = CreateCollector();

        collector.Tell(new QueryAllStats(), TestActor);

        var result = ExpectMsg<AllStatsResult>();
        Assert.Empty(result.Entries);
    }

    [Fact]
    public void UpdateStats_MultipleRuleSets_AllReturned()
    {
        var collector = CreateCollector();
        var stats1 = new ScoringStatsResult(DateTimeOffset.UtcNow, 0.3, 0.2, 5);
        var stats2 = new ScoringStatsResult(DateTimeOffset.UtcNow, 0.7, 0.5, 15);

        collector.Tell(new UpdateStats("rs-1", stats1));
        collector.Tell(new UpdateStats("rs-2", stats2));
        collector.Tell(new QueryAllStats(), TestActor);

        var result = ExpectMsg<AllStatsResult>();
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(0.3, result.Entries["rs-1"].MatchRate);
        Assert.Equal(0.7, result.Entries["rs-2"].MatchRate);
    }

    [Fact]
    public void UpdateStats_SameRuleSet_Replaces()
    {
        var collector = CreateCollector();
        var first = new ScoringStatsResult(DateTimeOffset.UtcNow, 0.3, 0.2, 5);
        var second = new ScoringStatsResult(DateTimeOffset.UtcNow, 0.9, 0.8, 20);

        collector.Tell(new UpdateStats("rs-1", first));
        collector.Tell(new UpdateStats("rs-1", second));
        collector.Tell(new QueryAllStats(), TestActor);

        var result = ExpectMsg<AllStatsResult>();
        Assert.Single(result.Entries);
        Assert.Equal(0.9, result.Entries["rs-1"].MatchRate);
    }

    [Fact]
    public void Backfill_OnPreStart_QueriesRegisteredRuleSets()
    {
        var actor = Sys.ActorOf(Props.Create<StatsCollector>());

        var query = _ruleSetResolverProbe.ExpectMsg<QueryRegisteredRuleSets>();
        Assert.NotNull(query);

        _ruleSetResolverProbe.Reply(new RegisteredRuleSetsResult([
            new RegisteredRuleSetEntry("rs-1", "Test Topic", [], null, null, null, null, null),
        ]));

        var historyQuery = _historyRegionProbe.ExpectMsg<QueryScoringStats>();
        Assert.Equal("rs-1", historyQuery.RuleSetId);

        _historyRegionProbe.Reply(new ScoringStatsResult(DateTimeOffset.UtcNow, 0.6, 0.4, 8));

        AwaitCondition(() =>
        {
            actor.Tell(new QueryAllStats(), TestActor);
            var r = ExpectMsg<AllStatsResult>();
            return r.Entries.Count == 1;
        }, TimeSpan.FromSeconds(3));

        actor.Tell(new QueryAllStats(), TestActor);
        var result = ExpectMsg<AllStatsResult>();
        Assert.Equal(0.6, result.Entries["rs-1"].MatchRate);
    }
}
