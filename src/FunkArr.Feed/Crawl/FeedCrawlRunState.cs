using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed class FeedCrawlRunState
{
    public CrawlInput Input { get; }

    public long ListTimestamp { get; }
    public CrawlWindow Window { get; }
    public RuleSetLookup Lookup { get; }
    public int PendingPages { get; private set; }
    public int NextOffset { get; private set; }
    public bool ReachedEnd { get; private set; }
    public bool HasPageError { get; private set; }
    public string? FirstPageError { get; private set; }
    public int TotalNewItems { get; private set; }
    public int TotalUnmatched { get; private set; }
    public List<FeedEntry> AcceptedEntries { get; } = [];
    public Dictionary<RejectionReason, int> RejectedByReason { get; } = [];
    public int PagesFired { get; private set; }

    public FeedCrawlRunState(CrawlInput input, long listTimestamp, CrawlWindow window, RuleSetLookup lookup)
    {
        Input = input;
        ListTimestamp = listTimestamp;
        Window = window;
        Lookup = lookup;
    }

    public bool HasMorePages => !ReachedEnd && PagesFired < Input.MaxPages;

    public void StartCrawling(int initialRequests)
    {
        PendingPages = initialRequests;
        PagesFired = initialRequests;
        NextOffset = initialRequests * Input.PageSize;
    }

    public ProcessPage NextPageRequest()
    {
        PendingPages++;
        PagesFired++;
        var offset = NextOffset;
        NextOffset += Input.PageSize;
        return new ProcessPage(offset, Window, Input.PageSize, Lookup, Input.MinConfidence);
    }

    public void Apply(PageProcessed result)
    {
        PendingPages--;
        ReachedEnd = ReachedEnd || result.ReachedEnd;

        if (result.Error is not null)
        {
            HasPageError = true;
            FirstPageError ??= result.Error;
            return;
        }

        TotalNewItems += result.NewItems;
        TotalUnmatched += result.Unmatched;
        AcceptedEntries.AddRange(result.Accepted);

        if (result.RejectedByReason is not null)
        {
            foreach (var (reason, count) in result.RejectedByReason)
            {
                RejectedByReason[reason] = RejectedByReason.GetValueOrDefault(reason) + count;
            }
        }
    }

    public CrawlResult GetCrawlResult(TimeSpan duration)
    {
        var published = AcceptedEntries.Count;
        var truncated = Window.Truncated;

        if (HasPageError && TotalNewItems == 0)
        {
            return CrawlResult.ForFailed(ListTimestamp, 0, 0, 0,
                truncated, duration, FirstPageError ?? "all pages failed");
        }

        if (HasPageError)
        {
            return CrawlResult.ForCompletedPartial(ListTimestamp, TotalNewItems, TotalUnmatched, published,
                duration);
        }

        return CrawlResult.ForCompleted(ListTimestamp, TotalNewItems, TotalUnmatched, published,
            truncated, duration);
    }
}
