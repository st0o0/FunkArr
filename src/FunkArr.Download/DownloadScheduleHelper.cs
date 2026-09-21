using FunkArr.Core;

namespace FunkArr.Download;

internal static class DownloadScheduleHelper
{
    internal static bool IsWithinSchedule(TimeOnly now, List<DownloadTimeSlot> schedule)
    {
        if (schedule.Count == 0)
        {
            return true;
        }

        foreach (var slot in schedule)
        {
            if (IsWithinSlot(now, slot))
            {
                return true;
            }
        }

        return false;
    }

    internal static TimeSpan DelayUntilNextWindow(TimeOnly now, List<DownloadTimeSlot> schedule)
    {
        if (schedule.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var minDelay = TimeSpan.MaxValue;

        foreach (var slot in schedule)
        {
            var delay = DelayUntil(now, slot.Start);
            if (delay < minDelay)
            {
                minDelay = delay;
            }
        }

        return minDelay;
    }

    private static bool IsWithinSlot(TimeOnly now, DownloadTimeSlot slot)
    {
        if (slot.Start < slot.End)
        {
            return now >= slot.Start && now < slot.End;
        }

        // Over-midnight: 23:00–02:00 means >= 23:00 OR < 02:00
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
