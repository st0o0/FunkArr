using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Tests;

public sealed class CrawlResultTests
{
    [Fact]
    public void ForCompleted_sets_status_and_fields()
    {
        var result = CrawlResult.ForCompleted(1000, 10, 2, 5, false, TimeSpan.FromSeconds(3));

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
    public void ForFailed_sets_error()
    {
        var result = CrawlResult.ForFailed(500, 3, 1, 0, true, TimeSpan.FromSeconds(1), "boom");

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Failed, result.Status),
            () => Assert.Equal("boom", result.Error),
            () => Assert.True(result.Gap));
    }

    [Fact]
    public void ForUnchanged_has_zero_counts()
    {
        var result = CrawlResult.ForUnchanged(800, TimeSpan.FromMilliseconds(50));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Unchanged, result.Status),
            () => Assert.Equal(800, result.ListTimestamp),
            () => Assert.Equal(0, result.NewItems),
            () => Assert.Equal(0, result.Published),
            () => Assert.False(result.Gap));
    }
}
