using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed record ProcessPage(
    int Offset,
    int PageSize,
    long LowerBound,
    long UpperBound,
    IReadOnlyDictionary<string, RuleSetTarget> TopicTargets,
    double MinConfidence);

internal abstract record ProcessPageResponse;

internal sealed record ProcessPageCompleted(
    FeedEntry[] Accepted,
    int NewItems,
    int Unmatched,
    bool ReachedEnd,
    IReadOnlyDictionary<RejectionReason, int> RejectedByReason) : ProcessPageResponse;

internal sealed record ProcessPageFailed(
    string Reason) : ProcessPageResponse;
