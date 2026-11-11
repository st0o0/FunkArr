using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed record GatedResult(
    FeedEntry[] Accepted,
    int SeenCount,
    int UnmatchedCount,
    IReadOnlyDictionary<RejectionReason, int> RejectedByReason);
