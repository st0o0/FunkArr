using FunkArr.Core;

namespace FunkArr.Download.Tests;

public sealed class DownloadScheduleHelperTests
{
    [Fact]
    public void IsWithinSchedule_empty_schedule_returns_true()
    {
        var result = DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(15, 0), []);

        Assert.True(result);
    }

    [Fact]
    public void IsWithinSchedule_normal_slot_inside_returns_true()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(8, 0), End = new TimeOnly(17, 0) }
        };

        Assert.True(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(12, 0), schedule));
    }

    [Fact]
    public void IsWithinSchedule_normal_slot_outside_returns_false()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(8, 0), End = new TimeOnly(17, 0) }
        };

        Assert.False(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(20, 0), schedule));
    }

    [Fact]
    public void IsWithinSchedule_normal_slot_at_start_boundary_returns_true()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(8, 0), End = new TimeOnly(17, 0) }
        };

        Assert.True(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(8, 0), schedule));
    }

    [Fact]
    public void IsWithinSchedule_normal_slot_at_end_boundary_returns_false()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(8, 0), End = new TimeOnly(17, 0) }
        };

        Assert.False(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(17, 0), schedule));
    }

    [Fact]
    public void IsWithinSchedule_over_midnight_before_midnight_returns_true()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
        };

        Assert.True(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(23, 30), schedule));
    }

    [Fact]
    public void IsWithinSchedule_over_midnight_after_midnight_returns_true()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
        };

        Assert.True(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(1, 0), schedule));
    }

    [Fact]
    public void IsWithinSchedule_over_midnight_during_day_returns_false()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
        };

        Assert.False(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(15, 0), schedule));
    }

    [Fact]
    public void IsWithinSchedule_multiple_slots_matches_second()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(6, 0), End = new TimeOnly(8, 0) },
            new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
        };

        Assert.True(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(7, 0), schedule));
        Assert.True(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(0, 30), schedule));
    }

    [Fact]
    public void IsWithinSchedule_multiple_slots_outside_all_returns_false()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(6, 0), End = new TimeOnly(8, 0) },
            new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
        };

        Assert.False(DownloadScheduleHelper.IsWithinSchedule(new TimeOnly(15, 0), schedule));
    }

    [Fact]
    public void DelayUntilNextWindow_returns_delay_to_nearest_start()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
        };

        var delay = DownloadScheduleHelper.DelayUntilNextWindow(new TimeOnly(15, 0), schedule);

        Assert.Equal(TimeSpan.FromHours(8), delay);
    }

    [Fact]
    public void DelayUntilNextWindow_wraps_past_midnight()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(6, 0), End = new TimeOnly(8, 0) }
        };

        var delay = DownloadScheduleHelper.DelayUntilNextWindow(new TimeOnly(10, 0), schedule);

        Assert.Equal(TimeSpan.FromHours(20), delay);
    }

    [Fact]
    public void DelayUntilNextWindow_multiple_slots_picks_nearest()
    {
        var schedule = new List<DownloadTimeSlot>
        {
            new() { Start = new TimeOnly(6, 0), End = new TimeOnly(8, 0) },
            new() { Start = new TimeOnly(23, 0), End = new TimeOnly(2, 0) }
        };

        var delay = DownloadScheduleHelper.DelayUntilNextWindow(new TimeOnly(15, 0), schedule);

        Assert.Equal(TimeSpan.FromHours(8), delay);
    }

    [Fact]
    public void DelayUntilNextWindow_empty_schedule_returns_zero()
    {
        var delay = DownloadScheduleHelper.DelayUntilNextWindow(new TimeOnly(15, 0), []);

        Assert.Equal(TimeSpan.Zero, delay);
    }
}
