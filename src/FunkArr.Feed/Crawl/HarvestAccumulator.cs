using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed class HarvestAccumulator
{
    public long ListTimestamp { get; }
    public bool Truncated { get; }
    public List<FeedEntry> Accepted { get; } = [];
    public int TotalSeen { get; private set; }
    public int TotalUnmatched { get; private set; }
    public Dictionary<RejectionReason, int> RejectedByReason { get; } = [];
    public bool HasEntries => Accepted.Count > 0;

    public HarvestAccumulator(long listTimestamp, bool truncated)
    {
        ListTimestamp = listTimestamp;
        Truncated = truncated;
    }

    public HarvestAccumulator Add(GatedResult gated)
    {
        Accepted.AddRange(gated.Accepted);
        TotalSeen += gated.SeenCount;
        TotalUnmatched += gated.UnmatchedCount;

        foreach (var (reason, count) in gated.RejectedByReason)
        {
            RejectedByReason[reason] = RejectedByReason.GetValueOrDefault(reason) + count;
        }

        return this;
    }

    public IngestSummary ToSummary(TimeSpan duration)
    {
        var status = Truncated ? CrawlOutcome.CompletedWithGap : CrawlOutcome.Completed;
        return new IngestSummary(status, ListTimestamp, TotalSeen, Accepted.Count,
            TotalUnmatched, Truncated, duration, null);
    }
}
