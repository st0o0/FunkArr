using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed record ProcessPage(
    int Offset,
    CrawlWindow Window,
    int PageSize,
    RuleSetLookup Lookup,
    double MinConfidence);

internal sealed record PageProcessed(
    FeedEntry[] Accepted,
    int NewItems,
    int Unmatched,
    bool ReachedEnd,
    IReadOnlyDictionary<RejectionReason, int>? RejectedByReason,
    string? Error);
