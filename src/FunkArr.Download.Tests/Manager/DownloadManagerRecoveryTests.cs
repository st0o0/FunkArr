using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Download.Manager;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests.Manager;

public sealed class DownloadManagerRecoveryTests : TestKit
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

    private readonly TestProbe _regionProbe;
    private readonly TestProbe _historyProbe;

    public DownloadManagerRecoveryTests() : base(_persistenceConfig)
    {
        _regionProbe = CreateTestProbe();
        _historyProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadRegion>(_regionProbe);
        registry.Register<IDownloadHistoryManager>(_historyProbe);
    }

    private IActorRef CreateManager(int concurrentDownloads = 3)
    {
        var manager = Sys.ActorOf(Props.Create(() => new DownloadManager(
            new TestOptionsMonitor<DownloadOptions>(
                new DownloadOptions { ConcurrentDownloads = concurrentDownloads }))));
        manager.Tell(new ScheduleEnabled());
        return manager;
    }

    private Guid EnqueueItem(IActorRef manager, string title = "Test", DownloadPriority priority = DownloadPriority.Normal)
    {
        manager.Tell(new AddDownload(
            new DownloadMedia(title, "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show),
            priority));
        var added = ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);
        return added.DownloadId;
    }

    [Fact]
    public void Queued_items_survive_restart()
    {
        var manager = CreateManager(concurrentDownloads: 0);
        var id1 = EnqueueItem(manager, "First");
        var id2 = EnqueueItem(manager, "Second");
        var id3 = EnqueueItem(manager, "Third");

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager(concurrentDownloads: 3);

        var dispatched = new HashSet<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var msg = _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
            dispatched.Add(msg.DownloadId);
        }

        Assert.Contains(id1, dispatched);
        Assert.Contains(id2, dispatched);
        Assert.Contains(id3, dispatched);
    }

    [Fact]
    public void ResetDispatched_on_recovery_returns_items_to_queue()
    {
        var manager = CreateManager(concurrentDownloads: 2);
        var id1 = EnqueueItem(manager, "First");
        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
        var id2 = EnqueueItem(manager, "Second");
        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager(concurrentDownloads: 2);

        var dispatched = new HashSet<Guid>();
        for (var i = 0; i < 2; i++)
        {
            var msg = _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
            dispatched.Add(msg.DownloadId);
        }

        Assert.Contains(id1, dispatched);
        Assert.Contains(id2, dispatched);
    }

    [Fact]
    public void Priority_order_preserved_after_recovery()
    {
        var manager = CreateManager(concurrentDownloads: 0);
        EnqueueItem(manager, "Low", DownloadPriority.Low);
        var normalId = EnqueueItem(manager, "Normal", DownloadPriority.Normal);
        var highId = EnqueueItem(manager, "High", DownloadPriority.High);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager(concurrentDownloads: 2);

        var first = _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
        var second = _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(highId, first.DownloadId);
        Assert.Equal(normalId, second.DownloadId);
    }

    [Fact]
    public void Pause_state_survives_restart()
    {
        var manager = CreateManager(concurrentDownloads: 3);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(cancellationToken: TestContext.Current.CancellationToken);

        EnqueueItem(manager, "Paused Item");

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager(concurrentDownloads: 3);

        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Snapshot_restore_produces_correct_state()
    {
        var manager = CreateManager(concurrentDownloads: 0);

        for (var i = 0; i < 26; i++)
        {
            EnqueueItem(manager, $"Item-{i}");
        }

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager(concurrentDownloads: 30);

        var dispatched = new HashSet<Guid>();
        for (var i = 0; i < 26; i++)
        {
            var msg = _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
            dispatched.Add(msg.DownloadId);
        }

        Assert.Equal(26, dispatched.Count);
    }
}
