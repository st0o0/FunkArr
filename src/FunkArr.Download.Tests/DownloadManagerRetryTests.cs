using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerRetryTests : TestKit
{
    private readonly TestProbe _regionProbe;

    public DownloadManagerRetryTests()
    {
        _regionProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadRegion>(_regionProbe);
    }

    private IActorRef CreateManager(int concurrentDownloads = 3)
    {
        var options = new TestOptionsMonitor<DownloadOptions>(
            new DownloadOptions { ConcurrentDownloads = concurrentDownloads });
        var routeResolver = new RouteResolver(
            new TestOptionsMonitor<RoutingOptions>(new RoutingOptions()));
        return Sys.ActorOf(Props.Create(() => new DownloadManager(options, routeResolver)));
    }

    private Guid EnqueueAndComplete(IActorRef manager)
    {
        manager.Tell(new AddDownload("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        var added = ExpectMsg<DownloadAdded>();

        // Manager sends InitDownload to region
        _regionProbe.ExpectMsg<InitDownload>();
        // Manager dispatches → StartDownload
        _regionProbe.ExpectMsg<StartDownload>();

        // Worker signals completion
        manager.Tell(new SlotFree(added.DownloadId));

        return added.DownloadId;
    }

    [Fact]
    public void Retry_re_enqueues_completed_download_and_sends_reset()
    {
        var manager = CreateManager(concurrentDownloads: 1);
        var id = EnqueueAndComplete(manager);

        manager.Tell(new RetryDownload(id));
        var result = ExpectMsg<RetryDownloadResult>();

        Assert.True(result.Success);
        Assert.Null(result.Error);

        // Manager should send ResetDownload to region
        _regionProbe.ExpectMsg<ResetDownload>(msg => msg.DownloadId == id);
        // Then dispatch → StartDownload
        _regionProbe.ExpectMsg<StartDownload>(msg => msg.DownloadId == id);
    }

    [Fact]
    public void Retry_rejects_when_already_queued()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        // Add first download — it gets dispatched immediately
        manager.Tell(new AddDownload("First", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        var first = ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();
        _regionProbe.ExpectMsg<StartDownload>();

        // Add second download — queued but not dispatched (slot full)
        manager.Tell(new AddDownload("Second", "https://example.com/v2.mp4", null, "ZDF", 1800, 500, MediaType.Show));
        var second = ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();

        // Retry the queued second download — should be rejected
        manager.Tell(new RetryDownload(second.DownloadId));
        var result = ExpectMsg<RetryDownloadResult>();

        Assert.False(result.Success);
        Assert.Equal("Item is already queued", result.Error);
    }

    [Fact]
    public void Retry_rejects_when_currently_dispatched()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new AddDownload("Active", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        var added = ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();
        _regionProbe.ExpectMsg<StartDownload>();

        // Download is dispatched (active) — retry should be rejected
        manager.Tell(new RetryDownload(added.DownloadId));
        var result = ExpectMsg<RetryDownloadResult>();

        Assert.False(result.Success);
        Assert.Equal("Item is already queued", result.Error);
    }

    [Fact]
    public void Retry_dispatches_immediately_when_slot_available()
    {
        var manager = CreateManager(concurrentDownloads: 3);
        var id = EnqueueAndComplete(manager);

        manager.Tell(new RetryDownload(id));
        ExpectMsg<RetryDownloadResult>();

        _regionProbe.ExpectMsg<ResetDownload>();
        // With 3 slots and nothing else running, it should dispatch immediately
        _regionProbe.ExpectMsg<StartDownload>(msg => msg.DownloadId == id);
    }

    [Fact]
    public void Retry_queues_when_no_slot_available()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        // Fill the slot
        manager.Tell(new AddDownload("Active", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        var active = ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();
        _regionProbe.ExpectMsg<StartDownload>();

        // Add and complete a second download by first freeing the slot, then re-filling
        manager.Tell(new SlotFree(active.DownloadId));
        manager.Tell(new AddDownload("Blocking", "https://example.com/v2.mp4", null, "ZDF", 1800, 500, MediaType.Show));
        var blocking = ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();
        _regionProbe.ExpectMsg<StartDownload>();

        // Complete the first download fully (it was already dequeued via SlotFree)
        // Now retry the first one — slot is taken by blocking
        manager.Tell(new RetryDownload(active.DownloadId));
        var result = ExpectMsg<RetryDownloadResult>();

        Assert.True(result.Success);

        _regionProbe.ExpectMsg<ResetDownload>(msg => msg.DownloadId == active.DownloadId);
        // Should NOT get StartDownload because slot is full
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }
}
