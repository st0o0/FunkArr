namespace FunkArr.Feed;

internal enum FeedRunStatus
{
    Unchanged,
    Completed,
    Failed
}

internal sealed record FeedRunOutcome(
    FeedRunStatus Status,
    long ListTimestamp,
    int NewItems,
    int Published,
    int Unmatched,
    bool Gap,
    TimeSpan Duration,
    string? Error = null);
