using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit.Xunit;
using FunkArr.Core.Configuration;
using FunkArr.Download.History;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests.History;

public sealed class DownloadHistoryManagerRecoveryTests : TestKit
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

    public DownloadHistoryManagerRecoveryTests() : base(_persistenceConfig) { }

    private IActorRef CreateManager()
    {
        return Sys.ActorOf(Props.Create(() => new DownloadHistoryManager(
            new TestOptionsMonitor<DownloadOptions>(new DownloadOptions { MaxHistoryRecords = 100 }),
            TimeProvider.System)));
    }

    private static RecordDownload MakeRecord(Guid? id = null, DownloadStatus status = DownloadStatus.Completed,
        long size = 500_000, int seconds = 60)
    {
        var downloadId = id ?? Guid.NewGuid();
        return new RecordDownload(downloadId, new DownloadCompletion(
            "Test Title", MediaType.Show, size, status, "tv/test/test.mkv", null, seconds,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    }

    [Fact]
    public void Recorded_entries_survive_restart()
    {
        var manager = CreateManager();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        manager.Tell(MakeRecord(id1));
        manager.Tell(MakeRecord(id2));
        manager.Tell(MakeRecord(id3));

        manager.Tell(new QueryHistory(0, 10));
        var result = ExpectMsg<HistoryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, result.TotalItems);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager();
        manager2.Tell(new QueryHistory(0, 10));
        var result2 = ExpectMsg<HistoryResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, result2.TotalItems);
        Assert.Contains(result2.Items, i => i.DownloadId == id1);
        Assert.Contains(result2.Items, i => i.DownloadId == id2);
        Assert.Contains(result2.Items, i => i.DownloadId == id3);
    }

    [Fact]
    public void Removed_entries_stay_removed_after_restart()
    {
        var manager = CreateManager();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        manager.Tell(MakeRecord(id1));
        manager.Tell(MakeRecord(id2));

        manager.Tell(new RemoveHistoryEntry(id1));
        ExpectMsg<DeleteDownloadResult>(cancellationToken: TestContext.Current.CancellationToken);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager();
        manager2.Tell(new QueryHistory(0, 10));
        var result = ExpectMsg<HistoryResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal(id2, result.Items[0].DownloadId);
    }

    [Fact]
    public void Stats_correct_after_recovery()
    {
        var manager = CreateManager();

        manager.Tell(MakeRecord(status: DownloadStatus.Completed, size: 1000, seconds: 100));
        manager.Tell(MakeRecord(status: DownloadStatus.Completed, size: 2000, seconds: 200));
        manager.Tell(MakeRecord(status: DownloadStatus.Failed, size: 500));

        manager.Tell(new QueryHistory(0, 10));
        var pre = ExpectMsg<HistoryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, pre.TotalItems);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager();
        manager2.Tell(new QueryHistoryStats());
        var stats = ExpectMsg<HistoryStatsResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, stats.TotalCompleted);
        Assert.Equal(1, stats.TotalFailed);
        Assert.Equal(3000, stats.TotalBytes);
    }

    [Fact]
    public void Snapshot_plus_events_restored_correctly()
    {
        var manager = CreateManager();

        var ids = new List<Guid>();
        for (var i = 0; i < 27; i++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            manager.Tell(MakeRecord(id));
        }

        manager.Tell(new QueryHistory(0, 30));
        var pre = ExpectMsg<HistoryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(27, pre.TotalItems);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager();
        manager2.Tell(new QueryHistory(0, 30));
        var result = ExpectMsg<HistoryResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(27, result.TotalItems);
    }
}
