using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit.Xunit;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class MatchHistoryWorkerTests : TestKit
{
    private static readonly Config _persistenceConfig = ConfigurationFactory.ParseString("""
        akka.persistence.journal.plugin = "akka.persistence.journal.inmem"
        akka.persistence.snapshot-store.plugin = "akka.persistence.snapshot-store.inmem"
        """);

    public MatchHistoryWorkerTests() : base(_persistenceConfig)
    {
    }

    private static string UniqueId() => Guid.NewGuid().ToString("N")[..12];

    private IActorRef CreateWorker(string ruleSetId, int maxSnapshots = 100, int maxAgeDays = 30, int snapshotInterval = 20) =>
        Sys.ActorOf(Props.Create(() => new MatchHistoryWorker(ruleSetId, maxSnapshots, maxAgeDays, snapshotInterval)));

    private static RecordScoringResult CreateRecord(string ruleSetId, Guid? requestId = null, DateTimeOffset? timestamp = null) =>
        new(
            RequestId: requestId ?? Guid.NewGuid(),
            RuleSetId: ruleSetId,
            Origin: new ScoringOrigin("test", "TestQuery"),
            Timestamp: timestamp ?? DateTimeOffset.UtcNow,
            CandidateCount: 1,
            MatchedCount: 1,
            ItemTraces:
            [
                new ItemTrace("Test Title", "Test", "ARD", 3600, 720, null, 0, true, 0.9, "test-rule", null, [])
            ]);

    [Fact]
    public void Record_persists_and_appears_in_state()
    {
        var id = UniqueId();
        var worker = CreateWorker(id);

        worker.Tell(CreateRecord(id));
        worker.Tell(new QueryScoringHistory(id, 0, 10));
        var result = ExpectMsg<ScoringHistoryResult>(TimeSpan.FromSeconds(5));

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Snapshots);
        Assert.Equal("test", result.Snapshots[0].Source);
        Assert.Equal("TestQuery", result.Snapshots[0].Query);
    }

    [Fact]
    public void Query_history_returns_paginated_summaries_newest_first()
    {
        var id = UniqueId();
        var worker = CreateWorker(id);
        var baseTime = DateTimeOffset.UtcNow.AddHours(-4);

        for (var i = 0; i < 5; i++)
        {
            worker.Tell(CreateRecord(id, timestamp: baseTime.AddHours(i)));
        }

        worker.Tell(new QueryScoringHistory(id, 0, 3));
        var page1 = ExpectMsg<ScoringHistoryResult>(TimeSpan.FromSeconds(5));
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.Snapshots.Length);
        Assert.True(page1.Snapshots[0].Timestamp > page1.Snapshots[1].Timestamp);
        Assert.True(page1.Snapshots[1].Timestamp > page1.Snapshots[2].Timestamp);

        worker.Tell(new QueryScoringHistory(id, 3, 3));
        var page2 = ExpectMsg<ScoringHistoryResult>();
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(2, page2.Snapshots.Length);
    }

    [Fact]
    public void Query_detail_returns_full_trace_for_known_request()
    {
        var id = UniqueId();
        var requestId = Guid.NewGuid();
        var worker = CreateWorker(id);

        worker.Tell(CreateRecord(id, requestId: requestId));
        worker.Tell(new QueryScoringDetail(id, requestId));
        var result = ExpectMsg<ScoringDetailResult>(TimeSpan.FromSeconds(5));

        Assert.Equal(requestId, result.RequestId);
        Assert.Equal("test", result.Source);
        Assert.Single(result.ItemTraces);
    }

    [Fact]
    public void Query_detail_returns_not_found_for_unknown_request()
    {
        var id = UniqueId();
        var worker = CreateWorker(id);

        var unknownId = Guid.NewGuid();
        worker.Tell(new QueryScoringDetail(id, unknownId));
        var result = ExpectMsg<ScoringDetailNotFound>();
        Assert.Equal(unknownId, result.RequestId);
    }

    [Fact]
    public void Retention_trims_by_max_count()
    {
        var id = UniqueId();
        var worker = CreateWorker(id, maxSnapshots: 3);

        for (var i = 0; i < 5; i++)
        {
            worker.Tell(CreateRecord(id));
        }

        worker.Tell(new QueryScoringHistory(id, 0, 10));
        var result = ExpectMsg<ScoringHistoryResult>(TimeSpan.FromSeconds(5));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public void Retention_trims_by_max_age()
    {
        var id = UniqueId();
        var worker = CreateWorker(id, maxAgeDays: 1);

        worker.Tell(CreateRecord(id, timestamp: DateTimeOffset.UtcNow.AddDays(-2)));
        worker.Tell(CreateRecord(id, timestamp: DateTimeOffset.UtcNow));
        worker.Tell(new QueryScoringHistory(id, 0, 10));
        var result = ExpectMsg<ScoringHistoryResult>(TimeSpan.FromSeconds(5));
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void Recovery_replays_and_trims()
    {
        var id = UniqueId();
        var worker = CreateWorker(id, maxSnapshots: 3);

        for (var i = 0; i < 5; i++)
        {
            worker.Tell(CreateRecord(id));
        }

        worker.Tell(new QueryScoringHistory(id, 0, 10));
        ExpectMsg<ScoringHistoryResult>(TimeSpan.FromSeconds(5));

        Watch(worker);
        Sys.Stop(worker);
        ExpectTerminated(worker);

        var recovered = CreateWorker(id, maxSnapshots: 3);
        recovered.Tell(new QueryScoringHistory(id, 0, 10));
        var result = ExpectMsg<ScoringHistoryResult>(TimeSpan.FromSeconds(5));
        Assert.Equal(3, result.TotalCount);
    }
}
