using Akka.Actor;
using Akka.Event;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Messages.Download;
using Microsoft.Extensions.Options;
using Terp.Akka;

namespace FunkArr.Download.Scheduler;

public sealed class DownloadScheduler : ReceiveActor, IWithTimers
{
    private const string TimerKey = "schedule";

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _downloadManager = Context.GetActor<IDownloadManager>();
    private readonly TimeProvider _timeProvider;
    private readonly IOptionsMonitor<DownloadOptions> _optionsMonitor;

    public ITimerScheduler Timers { get; set; } = null!;

    private sealed record Evaluate;

    public DownloadScheduler(TimeProvider timeProvider, IOptionsMonitor<DownloadOptions> options)
    {
        _timeProvider = timeProvider;
        _optionsMonitor = options;

        Receive<Evaluate>(_ => EvaluateSchedule());

        var self = Self;
        options.OnChange((_, _) => self.Tell(new Evaluate()));

        EvaluateSchedule();
    }

    private void EvaluateSchedule()
    {
        Timers.Cancel(TimerKey);

        var schedule = _optionsMonitor.CurrentValue.DownloadSchedule;

        if (schedule.Count == 0)
        {
            _log.Info("No download schedule configured, enabling downloads");
            Telemetry.ScheduleEnabled.Record(1);
            _downloadManager.Tell(new ScheduleEnabled());
            return;
        }

        var now = TimeOnly.FromTimeSpan(_timeProvider.GetLocalNow().TimeOfDay);

        if (now.IsWithinSchedule(schedule))
        {
            _log.Info("Within download schedule window, enabling downloads");
            Telemetry.ScheduleEnabled.Record(1);
            _downloadManager.Tell(new ScheduleEnabled());

            var delayToEnd = DelayUntilWindowEnd(now, schedule);
            Timers.StartSingleTimer(TimerKey, new Evaluate(), delayToEnd);
        }
        else
        {
            var delay = now.DelayUntilNextWindow(schedule);
            var nextWindow = _timeProvider.GetLocalNow().Add(delay);
            _log.Info("Outside download schedule, next window at {NextWindow}", nextWindow);
            Telemetry.ScheduleEnabled.Record(0);
            _downloadManager.Tell(new ScheduleDisabled(nextWindow));

            Timers.StartSingleTimer(TimerKey, new Evaluate(), delay);
        }
    }

    private static TimeSpan DelayUntilWindowEnd(TimeOnly now, List<DownloadTimeSlot> schedule)
    {
        var minDelay = TimeSpan.MaxValue;

        foreach (var slot in schedule)
        {
            if (!now.IsWithinSlot(slot))
            {
                continue;
            }

            var delay = now.DelayUntil(slot.End);
            if (delay < minDelay)
            {
                minDelay = delay;
            }
        }

        return minDelay == TimeSpan.MaxValue ? TimeSpan.FromHours(1) : minDelay;
    }
}
