using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Download;

public sealed class DownloadScheduler : ReceiveActor, IWithTimers
{
    private const string _timerKey = "schedule";

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
        Timers.Cancel(_timerKey);

        var schedule = _optionsMonitor.CurrentValue.DownloadSchedule;

        if (schedule.Count == 0)
        {
            _log.Info("No download schedule configured, enabling downloads");
            _downloadManager.Tell(new ScheduleEnabled());
            return;
        }

        var now = TimeOnly.FromTimeSpan(_timeProvider.GetLocalNow().TimeOfDay);

        if (DownloadScheduleHelper.IsWithinSchedule(now, schedule))
        {
            _log.Info("Within download schedule window, enabling downloads");
            _downloadManager.Tell(new ScheduleEnabled());

            var delayToEnd = DelayUntilWindowEnd(now, schedule);
            Timers.StartSingleTimer(_timerKey, new Evaluate(), delayToEnd);
        }
        else
        {
            var delay = DownloadScheduleHelper.DelayUntilNextWindow(now, schedule);
            var nextWindow = _timeProvider.GetLocalNow().Add(delay);
            _log.Info("Outside download schedule, next window at {NextWindow}", nextWindow);
            _downloadManager.Tell(new ScheduleDisabled(nextWindow));

            Timers.StartSingleTimer(_timerKey, new Evaluate(), delay);
        }
    }

    private static TimeSpan DelayUntilWindowEnd(TimeOnly now, List<DownloadTimeSlot> schedule)
    {
        var minDelay = TimeSpan.MaxValue;

        foreach (var slot in schedule)
        {
            if (!IsWithinSlot(now, slot))
            {
                continue;
            }

            var delay = DelayUntil(now, slot.End);
            if (delay < minDelay)
            {
                minDelay = delay;
            }
        }

        return minDelay == TimeSpan.MaxValue ? TimeSpan.FromHours(1) : minDelay;
    }

    private static bool IsWithinSlot(TimeOnly now, DownloadTimeSlot slot)
    {
        if (slot.Start < slot.End)
        {
            return now >= slot.Start && now < slot.End;
        }

        return now >= slot.Start || now < slot.End;
    }

    private static TimeSpan DelayUntil(TimeOnly now, TimeOnly target)
    {
        var diff = target - now;
        if (diff <= TimeSpan.Zero)
        {
            diff += TimeSpan.FromHours(24);
        }

        return diff;
    }
}
