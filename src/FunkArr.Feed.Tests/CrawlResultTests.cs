using FunkArr.Feed.Crawl;
using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Tests;

public sealed class CrawlResultTests
{
    [Fact]
    public void Completed_sets_status_and_fields()
    {
        var result = new IngestSummary(CrawlOutcome.Completed, 1000, 10, 5, 2, false, TimeSpan.FromSeconds(3), null);

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, result.Status),
            () => Assert.Equal(1000, result.ListTimestamp),
            () => Assert.Equal(10, result.ItemsFound),
            () => Assert.Equal(5, result.Accepted),
            () => Assert.Equal(2, result.Unmatched),
            () => Assert.False(result.Truncated),
            () => Assert.Null(result.Error));
    }

    [Fact]
    public void Failed_sets_error()
    {
        var result = new IngestSummary(CrawlOutcome.Failed, 500, 3, 0, 1, true, TimeSpan.FromSeconds(1), "boom");

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Failed, result.Status),
            () => Assert.Equal("boom", result.Error),
            () => Assert.True(result.Truncated));
    }

    [Fact]
    public void Unchanged_singleton_has_zero_counts()
    {
        var result = IngestSummary.Unchanged;

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Unchanged, result.Status),
            () => Assert.Equal(0, result.ListTimestamp),
            () => Assert.Equal(0, result.ItemsFound),
            () => Assert.Equal(0, result.Accepted),
            () => Assert.False(result.Truncated));
    }
}
