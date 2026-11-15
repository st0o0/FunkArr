using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Download.Manager;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests.Manager;

public sealed class DownloadManagerQueueScaleTests : TestKit
{
    private readonly TestProbe _regionProbe;

    public DownloadManagerQueueScaleTests()
    {
        _regionProbe = CreateTestProbe();
        var historyProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadRegion>(_regionProbe);
        registry.Register<IDownloadHistoryManager>(historyProbe);
    }

    private IActorRef CreateManager(int concurrentDownloads = 3)
    {
        var manager = Sys.ActorOf(Props.Create(() => new DownloadManager(
            new TestOptionsMonitor<DownloadOptions>(
                new DownloadOptions { ConcurrentDownloads = concurrentDownloads }))));
        manager.Tell(new ScheduleEnabled());
        return manager;
    }

    private static DownloadMedia Media(string title) =>
        new(title, "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show);

    [Fact]
    public void Queue_with_50_items_maintains_priority_ordering()
    {
        var state = new DownloadManagerState();

        var highIds = new List<Guid>();
        var normalIds = new List<Guid>();
        var lowIds = new List<Guid>();

        for (var i = 0; i < 10; i++)
        {
            var id = Guid.NewGuid();
            highIds.Add(id);
            state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.High, PersistedMediaType.Show));
        }

        for (var i = 0; i < 30; i++)
        {
            var id = Guid.NewGuid();
            normalIds.Add(id);
            state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal, PersistedMediaType.Show));
        }

        for (var i = 0; i < 10; i++)
        {
            var id = Guid.NewGuid();
            lowIds.Add(id);
            state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Low, PersistedMediaType.Show));
        }

        Assert.Equal(50, state.Queued.Count);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(DownloadPriority.High, state.Queued[i].Priority);
        }

        for (var i = 10; i < 40; i++)
        {
            Assert.Equal(DownloadPriority.Normal, state.Queued[i].Priority);
        }

        for (var i = 40; i < 50; i++)
        {
            Assert.Equal(DownloadPriority.Low, state.Queued[i].Priority);
        }

        Assert.Equal(highIds, [.. state.Queued.Take(10).Select(e => e.Id)]);
        Assert.Equal(normalIds, [.. state.Queued.Skip(10).Take(30).Select(e => e.Id)]);
        Assert.Equal(lowIds, [.. state.Queued.Skip(40).Select(e => e.Id)]);
    }

    [Fact]
    public void ForceStart_while_at_capacity_exceeds_max_concurrent()
    {
        var manager = CreateManager(concurrentDownloads: 2);

        manager.Tell(new AddDownload(Media("Show.S01E01"), DownloadPriority.Normal));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new AddDownload(Media("Show.S01E02"), DownloadPriority.Normal));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);

        // Drain all region messages from the first two adds (InitDownload + StartDownload per item)
        _regionProbe.ReceiveN(4, cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new AddDownload(Media("Show.S01E03"), DownloadPriority.Normal));
        var added3 = ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

        manager.Tell(new ForceStartDownload(added3.DownloadId));
        var result = ExpectMsg<ForceStartDownloadResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Rapid_enqueue_dequeue_100_items_no_corruption()
    {
        var state = new DownloadManagerState();
        var ids = new List<Guid>();

        for (var i = 0; i < 100; i++)
        {
            ids.Add(Guid.NewGuid());
        }

        foreach (var id in ids)
        {
            state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal, PersistedMediaType.Show));
        }

        Assert.Equal(100, state.Queued.Count);

        foreach (var id in ids)
        {
            state.Apply(new DownloadDequeued(id));
        }

        Assert.Empty(state.Queued);
        Assert.Empty(state.Dispatched);
        Assert.False(state.Contains(ids[0]));
    }

    [Fact]
    public void Rapid_dispatch_and_free_cycle_no_corruption()
    {
        var state = new DownloadManagerState();
        state.Apply(new ConcurrencyLimit(5));
        var ids = new List<Guid>();

        for (var i = 0; i < 20; i++)
        {
            ids.Add(Guid.NewGuid());
        }

        foreach (var id in ids)
        {
            state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal, PersistedMediaType.Show));
        }

        for (var batch = 0; batch < 4; batch++)
        {
            var batchIds = ids.Skip(batch * 5).Take(5).ToList();

            foreach (var id in batchIds)
            {
                state.Apply(new DownloadDispatched(id));
            }

            foreach (var id in batchIds)
            {
                state.Apply(new DownloadDequeued(id));
            }
        }

        Assert.Empty(state.Queued);
        Assert.Empty(state.Dispatched);
    }
}
