using FunkArr.Core;

namespace FunkArr.Feed;

public sealed record FeedSchedulerState(bool Enabled, TimeSpan Interval)
{
    public static readonly FeedSchedulerState Empty = new(true, TimeSpan.FromMinutes(15));
}

public static class FeedSchedulerStateExtensions
{
    public static FeedSchedulerState Apply(this FeedSchedulerState state, FeedOptions options) =>
        state with { Enabled = options.Enabled, Interval = options.Interval };
}
