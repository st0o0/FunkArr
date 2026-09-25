using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerScheduleGateTests : TestKit
{
    private readonly TestProbe _regionProbe;

    public DownloadManagerScheduleGateTests()
    {
        _regionProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadRegion>(_regionProbe);
    }

    private IActorRef CreateManager(int concurrentDownloads = 3) =>
        Sys.ActorOf(Props.Create(() => new DownloadManager(
            new TestOptionsMonitor<DownloadOptions>(
                new DownloadOptions { ConcurrentDownloads = concurrentDownloads }),
            new RouteResolver(
                new TestOptionsMonitor<RoutingOptions>(new RoutingOptions())))));

    [Fact]
    public void ScheduleDisabled_stops_dispatching()
    {
        var manager = CreateManager(concurrentDownloads: 1);
        var nextWindow = DateTimeOffset.UtcNow.AddHours(2);

        manager.Tell(new ScheduleDisabled(nextWindow));

        manager.Tell(new AddDownload("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        ExpectMsg<DownloadAdded>();

        _regionProbe.ExpectMsg<InitDownload>();
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public void ScheduleEnabled_resumes_dispatching()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new ScheduleDisabled(null));

        manager.Tell(new AddDownload("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();

        manager.Tell(new ScheduleEnabled());

        _regionProbe.ExpectMsg<StartDownload>();
    }

    [Fact]
    public void Both_gates_must_be_open_to_dispatch()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>();

        manager.Tell(new AddDownload("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();

        manager.Tell(new ScheduleEnabled());
        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public void Schedule_gate_independent_of_manual_gate()
    {
        var manager = CreateManager(concurrentDownloads: 1);

        manager.Tell(new ScheduleDisabled(null));

        manager.Tell(new AddDownload("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();

        manager.Tell(new ResumeDownloads());
        ExpectMsg<ResumeDownloadsResult>();

        _regionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public void ForceStart_works_when_schedule_disabled()
    {
        var manager = CreateManager(concurrentDownloads: 3);

        manager.Tell(new ScheduleDisabled(null));

        manager.Tell(new AddDownload("Test", "https://example.com/v.mp4", null, "ARD", 3600, 1000, MediaType.Show));
        var added = ExpectMsg<DownloadAdded>();
        _regionProbe.ExpectMsg<InitDownload>();

        manager.Tell(new ForceStartDownload(added.DownloadId));
        var result = ExpectMsg<ForceStartDownloadResult>();

        Assert.True(result.Success);
        _regionProbe.ExpectMsg<StartDownload>();
    }

    [Fact]
    public void QueryQueue_includes_pipeline_status()
    {
        var manager = CreateManager(concurrentDownloads: 3);
        var nextWindow = DateTimeOffset.UtcNow.AddHours(2);

        manager.Tell(new PauseDownloads());
        ExpectMsg<PauseDownloadsResult>();

        manager.Tell(new ScheduleDisabled(nextWindow));

        manager.Tell(new QueryQueue());
        var result = ExpectMsg<QueueResult>();

        Assert.True(result.IsPaused);
        Assert.False(result.IsScheduleActive);
        Assert.Equal(nextWindow, result.NextWindow);
    }
}
