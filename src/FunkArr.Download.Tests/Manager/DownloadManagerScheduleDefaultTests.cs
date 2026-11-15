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
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests.Manager;

public sealed class DownloadManagerScheduleDefaultTests : TestKit
{
    private readonly TestProbe _regionProbe;

    public DownloadManagerScheduleDefaultTests()
    {
        _regionProbe = CreateTestProbe();
        var historyProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadRegion>(_regionProbe);
        registry.Register<IDownloadHistoryManager>(historyProbe);
    }

    [Fact]
    public void Manager_does_not_dispatch_before_schedule_enabled()
    {
        var options = new TestOptionsMonitor<DownloadOptions>(
            new DownloadOptions { ConcurrentDownloads = 3 });
        var manager = Sys.ActorOf(Props.Create(() => new DownloadManager(options)));

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);

        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Manager_dispatches_after_schedule_enabled()
    {
        var options = new TestOptionsMonitor<DownloadOptions>(
            new DownloadOptions { ConcurrentDownloads = 3 });
        var manager = Sys.ActorOf(Props.Create(() => new DownloadManager(options)));

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);

        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

        manager.Tell(new ScheduleEnabled());

        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
    }
}
