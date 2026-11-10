using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Crawl;

internal sealed class FeedCrawlRunState
{
    public long ListTimestamp { get; }
    public long LowerBound { get; }
    public long UpperBound { get; }
    public bool Truncated { get; }
    public int PageSize { get; }
    public int MaxPages { get; }
    public double MinConfidence { get; }
    public long StartedAt { get; }
    public IReadOnlyDictionary<string, RuleSetTarget>? TopicTargets { get; private set; }

    public bool ReachedEnd { get; private set; }
    public bool HasPageError { get; private set; }
    public string? FirstPageError { get; private set; }
    public int TotalNewItems { get; private set; }
    public int TotalUnmatched { get; private set; }
    public List<FeedEntry> AcceptedEntries { get; } = [];
    public Dictionary<RejectionReason, int> RejectedByReason { get; } = [];

    private FeedCrawlRunState(
        long listTimestamp, long lowerBound, long upperBound, bool truncated,
        int pageSize, int maxPages, double minConfidence, long startedAt)
    {
        ListTimestamp = listTimestamp;
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Truncated = truncated;
        PageSize = pageSize;
        MaxPages = maxPages;
        MinConfidence = minConfidence;
        StartedAt = startedAt;
    }

    public static FeedCrawlRunState Init(
        long listTimestamp, long lowerBound, long upperBound, bool truncated,
        int pageSize, int maxPages, double minConfidence, long startedAt) =>
        new(listTimestamp, lowerBound, upperBound, truncated,
            pageSize, maxPages, minConfidence, startedAt);

    public void Apply(IReadOnlyList<RegisteredRuleSetEntry> entries)
    {
        var byKey = new Dictionary<string, RuleSetTarget>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var value = new RuleSetTarget(entry.RuleSetId, entry.MediaType ?? MediaType.Show, entry.Ids);

            foreach (var key in entry.Aliases.Prepend(entry.Topic))
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    byKey.TryAdd(key.Trim(), value);
                }
            }
        }

        TopicTargets = byKey;
    }

    public void Apply(IReadOnlyList<ProcessPageResponse> results)
    {
        foreach (var result in results)
        {
            Apply(result);
        }
    }

    public void Apply(ProcessPageResponse response)
    {
        switch (response)
        {
            case ProcessPageCompleted completed:
                ApplyCompleted(completed);
                break;
            case ProcessPageFailed failed:
                ApplyFailed(failed);
                break;
        }
    }

    private void ApplyCompleted(ProcessPageCompleted result)
    {
        ReachedEnd = ReachedEnd || result.ReachedEnd;
        TotalNewItems += result.NewItems;
        TotalUnmatched += result.Unmatched;
        AcceptedEntries.AddRange(result.Accepted);

        foreach (var (reason, count) in result.RejectedByReason)
        {
            RejectedByReason[reason] = RejectedByReason.GetValueOrDefault(reason) + count;
        }
    }

    private void ApplyFailed(ProcessPageFailed result)
    {
        HasPageError = true;
        FirstPageError ??= result.Reason;
    }

    public CrawlResult GetCrawlResult(TimeSpan duration)
    {
        var published = AcceptedEntries.Count;

        if (HasPageError && TotalNewItems == 0)
        {
            return new CrawlResult(CrawlOutcome.Failed, ListTimestamp, 0, 0, 0,
                Truncated, duration, FirstPageError ?? "all pages failed");
        }

        if (HasPageError)
        {
            return new CrawlResult(CrawlOutcome.CompletedWithGap, ListTimestamp, TotalNewItems, published,
                TotalUnmatched, true, duration, null);
        }

        return new CrawlResult(CrawlOutcome.Completed, ListTimestamp, TotalNewItems, published,
            TotalUnmatched, Truncated, duration, null);
    }
}
