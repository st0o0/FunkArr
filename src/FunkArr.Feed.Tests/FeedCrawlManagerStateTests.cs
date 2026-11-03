using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Tests;

public sealed class FeedCrawlManagerStateTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Completed_run_advances_cursor()
    {
        var state = FeedCrawlManagerState.Empty.Apply(new FeedCursorAdvanced(500, _now, 10, 4, 2, false));

        Assert.Multiple(
            () => Assert.Equal(500, state.Cursor),
            () => Assert.Equal(4, state.LastPublished),
            () => Assert.Equal("completed", state.LastOutcome),
            () => Assert.Equal(0, state.ConsecutiveFailures));
    }

    [Fact]
    public void Completed_with_gap()
    {
        var state = FeedCrawlManagerState.Empty.Apply(new FeedCursorAdvanced(500, _now, 10, 4, 2, true));

        Assert.Equal("completed_with_gap", state.LastOutcome);
    }

    [Fact]
    public void Completed_partial_advances_cursor_with_gap()
    {
        var state = FeedCrawlManagerState.Empty
            .Apply(new FeedRunFailed(_now, "error", 1))
            .Apply(new FeedRunFailed(_now, "error", 2))
            .Apply(new FeedCursorAdvanced(500, _now, 100, 10, 90, true));

        Assert.Multiple(
            () => Assert.Equal(500, state.Cursor),
            () => Assert.Equal(10, state.LastPublished),
            () => Assert.Equal("completed_with_gap", state.LastOutcome),
            () => Assert.Equal(0, state.ConsecutiveFailures),
            () => Assert.True(state.LastGap));
    }

    [Fact]
    public void Failed_run_keeps_cursor_and_counts_failures()
    {
        var state = (FeedCrawlManagerState.Empty with { Cursor = 100 })
            .Apply(new FeedRunFailed(_now, "boom", 1));

        Assert.Multiple(
            () => Assert.Equal(100, state.Cursor),
            () => Assert.Equal(1, state.ConsecutiveFailures),
            () => Assert.Equal("failed: boom", state.LastOutcome));
    }

    [Fact]
    public void Unchanged_run_updates_timestamp_only()
    {
        var state = FeedCrawlManagerState.Empty.WithUnchangedRun(_now);

        Assert.Multiple(
            () => Assert.Equal(_now, state.LastRunAt),
            () => Assert.Equal("unchanged", state.LastOutcome),
            () => Assert.Equal(0, state.Cursor));
    }

    [Fact]
    public void Status_reflects_state()
    {
        var state = FeedCrawlManagerState.Empty.Apply(new FeedCursorAdvanced(500, _now, 10, 4, 2, true));

        var status = state.ToStatus(enabled: true, running: false);

        Assert.Multiple(
            () => Assert.Equal(500, status.Cursor),
            () => Assert.Equal("completed_with_gap", status.LastOutcome),
            () => Assert.True(status.LastGap),
            () => Assert.Equal(2, status.LastUnmatched));
    }
}
