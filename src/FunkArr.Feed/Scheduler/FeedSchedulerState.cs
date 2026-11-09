using FunkArr.Core.Configuration;

namespace FunkArr.Feed.Scheduler;

public sealed class FeedSchedulerState
{
    public bool Enabled { get; private set; } = true;
    public TimeSpan Interval { get; private set; } = TimeSpan.FromMinutes(15);

    public static FeedSchedulerState Empty => new();

    public void Apply(FeedOptions options)
    {
        Enabled = options.Enabled;
        Interval = options.Interval;
    }
}
