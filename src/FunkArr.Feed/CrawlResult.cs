namespace FunkArr.Feed;

public enum CrawlStatus { Completed, Unchanged, Failed }

public sealed record CrawlInput(
    long Cursor,
    int PageSize,
    int MaxPages,
    int PagePrefetch,
    int MaxConcurrency,
    double MinConfidence,
    TimeSpan InitialLookback,
    TimeSpan MaxCatchUp,
    long StartedAt);

public sealed record CrawlWindow(long LowerBound, long UpperBound, bool Truncated = false);

public sealed record CrawlResult(
    CrawlStatus Status,
    long ListTimestamp,
    int NewItems,
    int Published,
    int Unmatched,
    bool Gap,
    TimeSpan Duration,
    string? Error)
{
    internal static CrawlResult ForCompleted(long ts, int items, int unmatched, int published,
        bool gap, TimeSpan duration) =>
        new(CrawlStatus.Completed, ts, items, published, unmatched, gap, duration, null);

    internal static CrawlResult ForFailed(long ts, int items, int unmatched, int published,
        bool gap, TimeSpan duration, string error) =>
        new(CrawlStatus.Failed, ts, items, published, unmatched, gap, duration, error);

    internal static CrawlResult ForUnchanged(long ts, TimeSpan duration) =>
        new(CrawlStatus.Unchanged, ts, 0, 0, 0, false, duration, null);
}
