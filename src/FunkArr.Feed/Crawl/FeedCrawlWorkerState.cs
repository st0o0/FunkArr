using FunkArr.Feed.Eval;
using FunkArr.Feed.Page;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Feed.Crawl;

internal sealed class FeedCrawlWorkerState(CrawlInput input)
{
    public CrawlInput Input { get; } = input;
    public long ListTimestamp { get; set; }
    public CrawlWindow? Window { get; set; }
    public RuleSetLookup? Lookup { get; set; }
    public List<MediathekItem> CollectedItems { get; } = [];
    public int PendingPages { get; set; }
    public int NextOffset { get; set; }
    public bool ReachedEnd { get; set; }
    public string? PageError { get; set; }
    public int TotalGroups { get; set; }
    public int PendingGroups { get; set; }
    public int FailedGroups { get; set; }
    public List<FeedEntry> AcceptedEntries { get; } = [];
    public string? FirstGroupError { get; set; }

    public void StartHarvesting(int initialRequests)
    {
        PendingPages = initialRequests;
        NextOffset = initialRequests * Input.PageSize;
    }

    public void Apply(PageResult page)
    {
        CollectedItems.AddRange(page.Items);
        PendingPages--;
        ReachedEnd = ReachedEnd || page.ReachedEnd;
        PageError ??= page.Error;
    }

    public void FireNextPage()
    {
        PendingPages++;
        NextOffset += Input.PageSize;
    }

    public void StartEvaluating(int totalGroups)
    {
        TotalGroups = totalGroups;
        PendingGroups = totalGroups;
    }

    public void Apply(GroupResult result)
    {
        AcceptedEntries.AddRange(result.Accepted);
        PendingGroups--;
        FailedGroups += result.Error is not null ? 1 : 0;
        FirstGroupError ??= result.Error;
    }

    public CrawlResult GetCrawlResult(TimeSpan duration)
    {
        var collected = CollectedItems.Count;
        var matched = TotalGroups;
        var unmatched = collected - matched;
        var published = AcceptedEntries.Count;
        var truncated = Window?.Truncated ?? false;

        if (PageError is not null && collected == 0)
        {
            return CrawlResult.ForFailed(ListTimestamp, 0, 0, 0,
                truncated, duration, PageError);
        }

        if (PageError is not null)
        {
            return CrawlResult.ForCompletedPartial(ListTimestamp, collected, unmatched, published,
                duration);
        }

        if (FailedGroups > 0)
        {
            return CrawlResult.ForFailed(ListTimestamp, collected, unmatched, published,
                truncated, duration,
                $"{FailedGroups} of {TotalGroups} ruleset groups failed");
        }

        if (FirstGroupError is not null)
        {
            return CrawlResult.ForFailed(ListTimestamp, collected, unmatched, published,
                truncated, duration, FirstGroupError);
        }

        return CrawlResult.ForCompleted(ListTimestamp, collected, unmatched, published,
            truncated, duration);
    }
}
