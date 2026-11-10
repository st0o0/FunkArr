using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Tests;

public sealed class CrawlResultTests
{
    [Fact]
    public void Completed_sets_status_and_fields()
    {
        var result = new CrawlResult(CrawlOutcome.Completed, 1000, 10, 5, 2, false, TimeSpan.FromSeconds(3), null);

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, result.Status),
            () => Assert.Equal(1000, result.ListTimestamp),
            () => Assert.Equal(10, result.NewItems),
            () => Assert.Equal(5, result.Published),
            () => Assert.Equal(2, result.Unmatched),
            () => Assert.False(result.Gap),
            () => Assert.Null(result.Error));
    }

    [Fact]
    public void Failed_sets_error()
    {
        var result = new CrawlResult(CrawlOutcome.Failed, 500, 3, 0, 1, true, TimeSpan.FromSeconds(1), "boom");

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Failed, result.Status),
            () => Assert.Equal("boom", result.Error),
            () => Assert.True(result.Gap));
    }

    [Fact]
    public void Unchanged_has_zero_counts()
    {
        var result = new CrawlResult(CrawlOutcome.Unchanged, 800, 0, 0, 0, false, TimeSpan.FromMilliseconds(50), null);

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Unchanged, result.Status),
            () => Assert.Equal(800, result.ListTimestamp),
            () => Assert.Equal(0, result.NewItems),
            () => Assert.Equal(0, result.Published),
            () => Assert.False(result.Gap));
    }
}
