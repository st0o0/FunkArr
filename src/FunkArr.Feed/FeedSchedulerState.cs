using FunkArr.Core;

namespace FunkArr.Feed;

public sealed class FeedSchedulerState
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

    public static FeedSchedulerState Empty => new();

    public void Apply(FeedOptions options)
    {
        Enabled = options.Enabled;
        Interval = options.Interval;
    }
}
