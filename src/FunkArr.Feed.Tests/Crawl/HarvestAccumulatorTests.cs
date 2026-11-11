using FunkArr.Feed.Crawl;
using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Tests.Crawl;

public sealed class HarvestAccumulatorTests
{
    [Fact]
    public void Empty_accumulator_has_no_entries()
    {
        var acc = new HarvestAccumulator(1000, false);

        Assert.Multiple(
            () => Assert.False(acc.HasEntries),
            () => Assert.Empty(acc.Accepted),
            () => Assert.Equal(0, acc.TotalSeen),
            () => Assert.Equal(0, acc.TotalUnmatched));
    }

    [Fact]
    public void Add_accumulates_entries_and_counts()
    {
        var acc = new HarvestAccumulator(1000, false);
        var entry = TestData.Entry("abc123", DateTimeOffset.UtcNow);

        acc.Add(new GatedResult([entry], 5, 2, new Dictionary<RejectionReason, int>
        {
            [RejectionReason.LowConfidence] = 1
        }));

        Assert.Multiple(
            () => Assert.True(acc.HasEntries),
            () => Assert.Single(acc.Accepted),
            () => Assert.Equal(5, acc.TotalSeen),
            () => Assert.Equal(2, acc.TotalUnmatched),
            () => Assert.Equal(1, acc.RejectedByReason[RejectionReason.LowConfidence]));
    }

    [Fact]
    public void Add_merges_rejections_across_groups()
    {
        var acc = new HarvestAccumulator(1000, false);

        acc.Add(new GatedResult([], 3, 1, new Dictionary<RejectionReason, int>
        {
            [RejectionReason.LowConfidence] = 2
        }));
        acc.Add(new GatedResult([], 4, 0, new Dictionary<RejectionReason, int>
        {
            [RejectionReason.LowConfidence] = 1,
            [RejectionReason.MissingEpisode] = 3
        }));

        Assert.Multiple(
            () => Assert.Equal(7, acc.TotalSeen),
            () => Assert.Equal(1, acc.TotalUnmatched),
            () => Assert.Equal(3, acc.RejectedByReason[RejectionReason.LowConfidence]),
            () => Assert.Equal(3, acc.RejectedByReason[RejectionReason.MissingEpisode]));
    }

    [Fact]
    public void ToSummary_completed_when_not_truncated()
    {
        var acc = new HarvestAccumulator(1000, false);
        var entry = TestData.Entry("abc123", DateTimeOffset.UtcNow);
        acc.Add(new GatedResult([entry], 5, 2, new Dictionary<RejectionReason, int>()));

        var summary = acc.ToSummary(TimeSpan.FromSeconds(3));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, summary.Status),
            () => Assert.Equal(1000, summary.ListTimestamp),
            () => Assert.Equal(5, summary.ItemsFound),
            () => Assert.Equal(1, summary.Accepted),
            () => Assert.Equal(2, summary.Unmatched),
            () => Assert.False(summary.Truncated),
            () => Assert.Equal(TimeSpan.FromSeconds(3), summary.Duration));
    }

    [Fact]
    public void ToSummary_completed_with_gap_when_truncated()
    {
        var acc = new HarvestAccumulator(1000, true);
        var entry = TestData.Entry("abc123", DateTimeOffset.UtcNow);
        acc.Add(new GatedResult([entry], 1, 0, new Dictionary<RejectionReason, int>()));

        var summary = acc.ToSummary(TimeSpan.FromSeconds(1));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.CompletedWithGap, summary.Status),
            () => Assert.True(summary.Truncated));
    }
}
