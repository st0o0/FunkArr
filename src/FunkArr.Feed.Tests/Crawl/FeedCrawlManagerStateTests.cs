using FunkArr.Feed.Crawl;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Tests.Crawl;

public sealed class FeedCrawlManagerStateTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Completed_run_advances_cursor()
    {
        var state = new FeedCrawlManagerState();
        state.Apply(new FeedCursorAdvanced(500, _now, 10, 4, 2, false));

        Assert.Multiple(
            () => Assert.Equal(500, state.Cursor),
            () => Assert.Equal(4, state.LastPublished),
            () => Assert.Equal(CrawlOutcome.Completed, state.LastOutcome),
            () => Assert.Equal(0, state.ConsecutiveFailures));
    }

    [Fact]
    public void Completed_with_gap()
    {
        var state = new FeedCrawlManagerState();
        state.Apply(new FeedCursorAdvanced(500, _now, 10, 4, 2, true));

        Assert.Equal(CrawlOutcome.CompletedWithGap, state.LastOutcome);
    }

    [Fact]
    public void Completed_partial_advances_cursor_with_gap()
    {
        var state = new FeedCrawlManagerState();
        state.Apply(new FeedRunFailed(_now, "error", 1));
        state.Apply(new FeedRunFailed(_now, "error", 2));
        state.Apply(new FeedCursorAdvanced(500, _now, 100, 10, 90, true));

        Assert.Multiple(
            () => Assert.Equal(500, state.Cursor),
            () => Assert.Equal(10, state.LastPublished),
            () => Assert.Equal(CrawlOutcome.CompletedWithGap, state.LastOutcome),
            () => Assert.Equal(0, state.ConsecutiveFailures),
            () => Assert.True(state.LastGap));
    }

    [Fact]
    public void Failed_run_keeps_cursor_and_counts_failures()
    {
        var state = FeedCrawlManagerState.FromPersistence(
            new PersistedFeedCrawlState(100, null, PersistedCrawlOutcome.Completed, null, 0, 0, 0, false, 0));
        state.Apply(new FeedRunFailed(_now, "boom", 1));

        Assert.Multiple(
            () => Assert.Equal(100, state.Cursor),
            () => Assert.Equal(1, state.ConsecutiveFailures),
            () => Assert.Equal(CrawlOutcome.Failed, state.LastOutcome),
            () => Assert.Equal("boom", state.LastError));
    }

    [Fact]
    public void Unchanged_run_updates_timestamp_only()
    {
        var state = new FeedCrawlManagerState();
        state.WithUnchangedRun(_now);

        Assert.Multiple(
            () => Assert.Equal(_now, state.LastRunAt),
            () => Assert.Equal(CrawlOutcome.Unchanged, state.LastOutcome),
            () => Assert.Equal(0, state.Cursor));
    }

    [Fact]
    public void Status_reflects_state()
    {
        var state = new FeedCrawlManagerState();
        state.Apply(new FeedCursorAdvanced(500, _now, 10, 4, 2, true));

        var status = state.ToStatus(enabled: true, running: false);

        Assert.Multiple(
            () => Assert.Equal(500, status.Cursor),
            () => Assert.Equal(CrawlOutcome.CompletedWithGap, status.LastOutcome),
            () => Assert.True(status.LastGap),
            () => Assert.Equal(2, status.LastUnmatched));
    }
}
