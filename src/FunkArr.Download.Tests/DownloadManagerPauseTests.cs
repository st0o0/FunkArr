using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerPauseTests : TestKit
{
    private readonly TestProbe _regionProbe;

    public DownloadManagerPauseTests()
    {
        _regionProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadRegion>(_regionProbe);
    }

    private IActorRef CreateManager(int concurrentDownloads = 3) =>
        Sys.ActorOf(Props.Create(() => new DownloadManager(
            new TestOptionsMonitor<DownloadOptions>(
                new DownloadOptions { ConcurrentDownloads = concurrentDownloads }))));

    [Fact]
    public void Pause_stops_new_dispatches()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(r => r.Success, cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);

        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Resume_dispatches_queued_downloads()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ResumeDownloads());
        ExpectMsg<ResumeDownloadsResult>(r => r.Success, cancellationToken: TestContext.Current.CancellationToken);

        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Pause_is_idempotent()
    {
        var manager = CreateManager();

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(r => r.Success, cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(r => r.Success, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Resume_is_idempotent()
    {
        var manager = CreateManager();

        manager.Tell(new ResumeDownloads());
        ExpectMsg<ResumeDownloadsResult>(r => r.Success, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Graceful_drain_running_downloads_complete()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        var added = ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new SlotFree(added.DownloadId));
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ForceStart_dispatches_while_paused()
    {
        var manager = CreateManager(concurrentDownloads: 3);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        var added = ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ForceStartDownload(added.DownloadId));
        var result = ExpectMsg<ForceStartDownloadResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _regionProbe.ExpectMsg<StartDownload>(msg => msg.DownloadId == added.DownloadId, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ForceStart_rejects_unknown_id()
    {
        var manager = CreateManager();

        manager.Tell(new ForceStartDownload(Guid.NewGuid()));
        var result = ExpectMsg<ForceStartDownloadResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Item not queued", result.Error);
    }

    [Fact]
    public void ForceStart_rejects_already_dispatched()
    {
        var manager = CreateManager(concurrentDownloads: 3);

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        var added = ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ForceStartDownload(added.DownloadId));
        var result = ExpectMsg<ForceStartDownloadResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Item already dispatched", result.Error);
    }
}
