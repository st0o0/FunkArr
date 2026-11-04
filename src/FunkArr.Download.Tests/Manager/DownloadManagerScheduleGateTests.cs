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

public sealed class DownloadManagerScheduleGateTests : TestKit
{
    private readonly TestProbe _regionProbe;

    public DownloadManagerScheduleGateTests()
    {
        _regionProbe = CreateTestProbe();
        var historyProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadRegion>(_regionProbe);
        registry.Register<IDownloadHistoryManager>(historyProbe);
    }

    private IActorRef CreateManager(int concurrentDownloads = 3) =>
        Sys.ActorOf(Props.Create(() => new DownloadManager(
            new TestOptionsMonitor<DownloadOptions>(
                new DownloadOptions { ConcurrentDownloads = concurrentDownloads }))));

    [Fact]
    public void ScheduleDisabled_stops_dispatching()
    {
        var manager = CreateManager(concurrentDownloads: 1);
        var nextWindow = DateTimeOffset.UtcNow.AddHours(2);

        manager.Tell(new ScheduleDisabled(nextWindow));

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);

        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ScheduleEnabled_resumes_dispatching()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new ScheduleDisabled(null));

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ScheduleEnabled());

        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Both_gates_must_be_open_to_dispatch()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ScheduleEnabled());
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Schedule_gate_independent_of_manual_gate()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new ScheduleDisabled(null));

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ResumeDownloads());
        ExpectMsg<ResumeDownloadsResult>(cancellationToken: TestContext.Current.CancellationToken);

        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ForceStart_works_when_schedule_disabled()
    {
        var manager = CreateManager(concurrentDownloads: 3);

        manager.Tell(new ScheduleDisabled(null));

        manager.Tell(new AddDownload(new DownloadMedia("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show)));
        var added = ExpectMsg<DownloadAdded>(cancellationToken: TestContext.Current.CancellationToken);
        _regionProbe.ExpectMsg<InitDownload>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ForceStartDownload(added.DownloadId));
        var result = ExpectMsg<ForceStartDownloadResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _regionProbe.ExpectMsg<StartDownload>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void QueryQueue_includes_pipeline_status()
    {
        var manager = CreateManager(concurrentDownloads: 3);
        var nextWindow = DateTimeOffset.UtcNow.AddHours(2);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new ScheduleDisabled(nextWindow));

        manager.Tell(new QueryQueue());
        var result = ExpectMsg<QueueResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsPaused);
        Assert.False(result.IsScheduleActive);
        Assert.Equal(nextWindow, result.NextWindow);
    }
}
