using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Tests;

public sealed class FeedIngestStateTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static FeedRunOutcome Outcome(FeedRunStatus status, long listTimestamp = 500, bool gap = false) =>
        new(status, listTimestamp, 10, 4, 2, gap, TimeSpan.FromSeconds(3), status == FeedRunStatus.Failed ? "boom" : null);

    [Fact]
    public void Completed_run_advances_cursor()
    {
        var evt = Assert.IsType<FeedCursorAdvanced>(FeedIngestState.Empty.Decide(Outcome(FeedRunStatus.Completed), _now));

        var state = FeedIngestState.Empty.Apply(evt);

        Assert.Multiple(
            () => Assert.Equal(500, state.Cursor),
            () => Assert.Equal(4, state.LastPublished),
            () => Assert.Equal("completed", state.LastOutcome),
            () => Assert.Equal(0, state.ConsecutiveFailures));
    }

    [Fact]
    public void Unchanged_run_persists_nothing()
    {
        Assert.Null(FeedIngestState.Empty.Decide(Outcome(FeedRunStatus.Unchanged), _now));
    }

    [Fact]
    public void Failed_run_keeps_cursor_and_counts_failures()
    {
        var state = FeedIngestState.Empty with { Cursor = 100 };

        var evt = Assert.IsType<FeedRunFailed>(state.Decide(Outcome(FeedRunStatus.Failed), _now));
        state = state.Apply(evt);

        Assert.Multiple(
            () => Assert.Equal(100, state.Cursor),
            () => Assert.Equal(1, state.ConsecutiveFailures),
            () => Assert.Equal("failed: boom", state.LastOutcome));
    }

    [Fact]
    public void Third_failure_advances_cursor_with_gap()
    {
        var state = FeedIngestState.Empty with { Cursor = 100, ConsecutiveFailures = 2 };

        var evt = Assert.IsType<FeedCursorAdvanced>(state.Decide(Outcome(FeedRunStatus.Failed), _now));

        Assert.Multiple(
            () => Assert.Equal(500, evt.Cursor),
            () => Assert.True(evt.Gap),
            () => Assert.Equal(0, state.Apply(evt).ConsecutiveFailures));
    }

    [Fact]
    public void Third_failure_without_list_timestamp_keeps_cursor()
    {
        var state = FeedIngestState.Empty with { Cursor = 100, ConsecutiveFailures = 2 };

        var evt = Assert.IsType<FeedRunFailed>(state.Decide(Outcome(FeedRunStatus.Failed, listTimestamp: 0), _now));

        Assert.Equal(3, evt.ConsecutiveFailures);
    }

    [Fact]
    public void Status_reflects_state()
    {
        var state = FeedIngestState.Empty.Apply(new FeedCursorAdvanced(500, _now, 10, 4, 2, true));

        var status = state.ToStatus(enabled: true, running: false);

        Assert.Multiple(
            () => Assert.Equal(500, status.Cursor),
            () => Assert.Equal("completed_with_gap", status.LastOutcome),
            () => Assert.True(status.LastGap),
            () => Assert.Equal(2, status.LastUnmatched));
    }
}
