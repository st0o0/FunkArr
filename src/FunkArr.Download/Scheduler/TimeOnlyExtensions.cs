using FunkArr.Core.Configuration;

namespace FunkArr.Download.Scheduler;

internal static class TimeOnlyExtensions
{
    internal static bool IsWithinSchedule(this TimeOnly now, List<DownloadTimeSlot> schedule)
    {
        if (schedule.Count == 0)
        {
            return true;
        }

        foreach (var slot in schedule)
        {
            if (now.IsWithinSlot(slot))
            {
                return true;
            }
        }

        return false;
    }

    internal static TimeSpan DelayUntilNextWindow(this TimeOnly now, List<DownloadTimeSlot> schedule)
    {
        if (schedule.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var minDelay = TimeSpan.MaxValue;

        foreach (var slot in schedule)
        {
            var delay = now.DelayUntil(slot.Start);
            if (delay < minDelay)
            {
                minDelay = delay;
            }
        }

        return minDelay;
    }

    internal static bool IsWithinSlot(this TimeOnly now, DownloadTimeSlot slot)
    {
        if (slot.Start < slot.End)
        {
            return now >= slot.Start && now < slot.End;
        }

        // Over-midnight: 23:00–02:00 means >= 23:00 OR < 02:00
        return now >= slot.Start || now < slot.End;
    }

    internal static TimeSpan DelayUntil(this TimeOnly now, TimeOnly target)
    {
        var diff = target - now;
        if (diff <= TimeSpan.Zero)
        {
            diff += TimeSpan.FromHours(24);
        }

        return diff;
    }
}
