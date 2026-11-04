using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Download.Scheduler;
using FunkArr.Messages.Download;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Time.Testing;

namespace FunkArr.Download.Tests.Scheduler;

public sealed class DownloadSchedulerTests : TestKit
{
    private readonly TestProbe _managerProbe;

    public DownloadSchedulerTests()
    {
        _managerProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IDownloadManager>(_managerProbe);
    }

    [Fact]
    public void No_schedule_sends_enabled_on_startup()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 22, 15, 0, 0, TimeSpan.FromHours(2)));
        var options = new TestOptionsMonitor<DownloadOptions>(new DownloadOptions());

        Sys.ActorOf(Props.Create(() => new DownloadScheduler(time, options)));

        _managerProbe.ExpectMsg<ScheduleEnabled>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Within_schedule_sends_enabled_on_startup()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 22, 23, 30, 0, TimeSpan.FromHours(2)));
        var options = new TestOptionsMonitor<DownloadOptions>(new DownloadOptions
        {
            DownloadSchedule = [new DownloadTimeSlot { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }]
        });

        Sys.ActorOf(Props.Create(() => new DownloadScheduler(time, options)));

        _managerProbe.ExpectMsg<ScheduleEnabled>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Outside_schedule_sends_disabled_with_next_window()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 22, 15, 0, 0, TimeSpan.FromHours(2)));
        var options = new TestOptionsMonitor<DownloadOptions>(new DownloadOptions
        {
            DownloadSchedule = [new DownloadTimeSlot { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }]
        });

        Sys.ActorOf(Props.Create(() => new DownloadScheduler(time, options)));

        var msg = _managerProbe.ExpectMsg<ScheduleDisabled>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(msg.NextWindow);
    }

    [Fact]
    public void Config_change_removes_schedule_sends_enabled()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 22, 15, 0, 0, TimeSpan.FromHours(2)));
        var options = new TestOptionsMonitor<DownloadOptions>(new DownloadOptions
        {
            DownloadSchedule = [new DownloadTimeSlot { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }]
        });

        Sys.ActorOf(Props.Create(() => new DownloadScheduler(time, options)));
        _managerProbe.ExpectMsg<ScheduleDisabled>(cancellationToken: TestContext.Current.CancellationToken);

        options.Update(new DownloadOptions());
        _managerProbe.ExpectMsg<ScheduleEnabled>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Config_change_adds_schedule_sends_disabled()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 22, 15, 0, 0, TimeSpan.FromHours(2)));
        var options = new TestOptionsMonitor<DownloadOptions>(new DownloadOptions());

        Sys.ActorOf(Props.Create(() => new DownloadScheduler(time, options)));
        _managerProbe.ExpectMsg<ScheduleEnabled>(cancellationToken: TestContext.Current.CancellationToken);

        options.Update(new DownloadOptions
        {
            DownloadSchedule = [new DownloadTimeSlot { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }]
        });

        _managerProbe.ExpectMsg<ScheduleDisabled>(cancellationToken: TestContext.Current.CancellationToken);
    }
}
