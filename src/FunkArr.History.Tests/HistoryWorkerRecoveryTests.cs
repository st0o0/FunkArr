using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Messages;
using FunkArr.Messages.History;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Microsoft.Extensions.Options;

namespace FunkArr.History.Tests;

public sealed class HistoryWorkerRecoveryTests : TestKit
{
    private static readonly Config _persistenceConfig = ConfigurationFactory.ParseString("""
        akka.persistence {
            journal {
                plugin = "akka.persistence.journal.inmem"
                inmem {
                    class = "Akka.Persistence.Journal.MemoryJournal, Akka.Persistence"
                    plugin-dispatcher = "akka.actor.default-dispatcher"
                }
            }
            snapshot-store {
                plugin = "akka.persistence.snapshot-store.inmem"
                inmem {
                    class = "Akka.Persistence.Snapshot.MemorySnapshotStore, Akka.Persistence"
                    plugin-dispatcher = "akka.actor.default-dispatcher"
                }
            }
        }
        """);

    private static readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private readonly TestProbe _statsCollectorProbe;
    private ScoringHistoryOptions _options;

    public HistoryWorkerRecoveryTests() : base(_persistenceConfig)
    {
        _statsCollectorProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IStatsCollector>(_statsCollectorProbe);
        _options = new ScoringHistoryOptions
        {
            MaxSnapshots = 100,
            MaxAgeDays = 30,
            SnapshotInterval = 20
        };
    }

    private IActorRef CreateWorker(string ruleSetId = "rs-test")
    {
        var optionsMonitor = new TestOptionsMonitor(_options);
        return Sys.ActorOf(Props.Create(() => new HistoryWorker(ruleSetId, optionsMonitor)));
    }

    private void RecordEntry(IActorRef worker, Guid? requestId = null, int candidateCount = 5,
        int matchedCount = 2, int enrichedCount = 1, DateTimeOffset? timestamp = null)
    {
        var id = requestId ?? Guid.NewGuid();
        var cmd = new RecordHistory(id, "rs-test",
            new ScoringOrigin(SearchSource.Sonarr, "test-query"),
            timestamp ?? _now, candidateCount, matchedCount, enrichedCount, []);
        worker.Tell(cmd, TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Recorded_entries_survive_restart()
    {
        var worker = CreateWorker();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        RecordEntry(worker, id1);
        RecordEntry(worker, id2);
        RecordEntry(worker, id3);

        Watch(worker);
        Sys.Stop(worker);
        ExpectTerminated(worker, cancellationToken: TestContext.Current.CancellationToken);

        var worker2 = CreateWorker();
        worker2.Tell(new QueryScoringHistory("rs-test", 0, 10));
        var result = ExpectMsg<ScoringHistoryResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalCount);
        Assert.Contains(result.Snapshots, s => s.RequestId == id1);
        Assert.Contains(result.Snapshots, s => s.RequestId == id2);
        Assert.Contains(result.Snapshots, s => s.RequestId == id3);
    }

    [Fact]
    public void Stats_correct_after_replay()
    {
        var worker = CreateWorker();

        RecordEntry(worker, candidateCount: 10, matchedCount: 5, enrichedCount: 3);
        RecordEntry(worker, candidateCount: 10, matchedCount: 8, enrichedCount: 4);

        Watch(worker);
        Sys.Stop(worker);
        ExpectTerminated(worker, cancellationToken: TestContext.Current.CancellationToken);

        var worker2 = CreateWorker();
        worker2.Tell(new QueryScoringStats("rs-test"));
        var result = ExpectMsg<ScoringStatsQueryResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Stats.TotalRuns);
        Assert.NotNull(result.Stats.MatchRate);
        Assert.NotNull(result.Stats.EnrichmentRate);
    }

    [Fact]
    public void OnReplaySuccess_trims_old_entries()
    {
        _options = new ScoringHistoryOptions
        {
            MaxSnapshots = 5,
            MaxAgeDays = 30,
            SnapshotInterval = 20
        };

        var worker = CreateWorker();

        for (var i = 0; i < 10; i++)
        {
            RecordEntry(worker, timestamp: _now.AddHours(i));
        }

        Watch(worker);
        Sys.Stop(worker);
        ExpectTerminated(worker, cancellationToken: TestContext.Current.CancellationToken);

        var worker2 = CreateWorker();
        worker2.Tell(new QueryScoringHistory("rs-test", 0, 20));
        var result = ExpectMsg<ScoringHistoryResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public void Snapshot_plus_events_restored_correctly()
    {
        _options = new ScoringHistoryOptions
        {
            MaxSnapshots = 100,
            MaxAgeDays = 30,
            SnapshotInterval = 5
        };

        var worker = CreateWorker();

        for (var i = 0; i < 7; i++)
        {
            RecordEntry(worker);
        }

        Watch(worker);
        Sys.Stop(worker);
        ExpectTerminated(worker, cancellationToken: TestContext.Current.CancellationToken);

        var worker2 = CreateWorker();
        worker2.Tell(new QueryScoringHistory("rs-test", 0, 20));
        var result = ExpectMsg<ScoringHistoryResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(7, result.TotalCount);
    }

    private sealed class TestOptionsMonitor(ScoringHistoryOptions options) : IOptionsMonitor<ScoringHistoryOptions>
    {
        public ScoringHistoryOptions CurrentValue => options;
        public ScoringHistoryOptions Get(string? name) => options;
        public IDisposable? OnChange(Action<ScoringHistoryOptions, string?> listener) => null;
    }
}
