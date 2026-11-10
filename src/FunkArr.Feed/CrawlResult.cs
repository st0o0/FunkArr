using FunkArr.Messages.Feed;

namespace FunkArr.Feed;

internal sealed record CrawlResult(
    CrawlOutcome Status,
    long ListTimestamp,
    int NewItems,
    int Published,
    int Unmatched,
    bool Gap,
    TimeSpan Duration,
    string? Error);
