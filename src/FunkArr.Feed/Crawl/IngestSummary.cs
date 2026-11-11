using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed record IngestSummary(
    CrawlOutcome Status,
    long ListTimestamp,
    int ItemsFound,
    int Accepted,
    int Unmatched,
    bool Truncated,
    TimeSpan Duration,
    string? Error)
{
    public static IngestSummary Unchanged { get; } =
        new(CrawlOutcome.Unchanged, 0, 0, 0, 0, false, TimeSpan.Zero, null);
}
