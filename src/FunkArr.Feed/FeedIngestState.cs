using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed;

public sealed record FeedIngestState(
    long Cursor,
    DateTimeOffset? LastRunAt,
    string? LastOutcome,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures)
{
    internal const int MaxConsecutiveFailures = 3;

    public static readonly FeedIngestState Empty = new(0, null, null, 0, 0, 0, false, 0);

    public static FeedIngestState FromPersistence(PersistedFeedIngestState persisted) =>
        new(persisted.Cursor, persisted.LastRunAt, persisted.LastOutcome, persisted.LastNewItems,
            persisted.LastPublished, persisted.LastUnmatched, persisted.LastGap, persisted.ConsecutiveFailures);
}

public static class FeedIngestStateExtensions
{
    public static FeedIngestState Apply(this FeedIngestState state, FeedCursorAdvanced evt) =>
        state with
        {
            Cursor = evt.Cursor,
            LastRunAt = evt.CompletedAt,
            LastOutcome = evt.Gap ? "completed_with_gap" : "completed",
            LastNewItems = evt.NewItems,
            LastPublished = evt.Published,
            LastUnmatched = evt.Unmatched,
            LastGap = evt.Gap,
            ConsecutiveFailures = 0
        };

    public static FeedIngestState Apply(this FeedIngestState state, FeedRunFailed evt) =>
        state with
        {
            LastRunAt = evt.FailedAt,
            LastOutcome = $"failed: {evt.Reason}",
            ConsecutiveFailures = evt.ConsecutiveFailures
        };

    public static FeedIngestState WithUnchangedRun(this FeedIngestState state, DateTimeOffset at) =>
        state with { LastRunAt = at, LastOutcome = "unchanged" };

    internal static object? Decide(this FeedIngestState state, FeedRunOutcome outcome, DateTimeOffset now)
    {
        switch (outcome.Status)
        {
            case FeedRunStatus.Completed:
                return new FeedCursorAdvanced(outcome.ListTimestamp, now, outcome.NewItems, outcome.Published,
                    outcome.Unmatched, outcome.Gap);

            case FeedRunStatus.Failed:
                var failures = state.ConsecutiveFailures + 1;
                if (failures >= FeedIngestState.MaxConsecutiveFailures && outcome.ListTimestamp > state.Cursor)
                {
                    return new FeedCursorAdvanced(outcome.ListTimestamp, now, outcome.NewItems, outcome.Published,
                        outcome.Unmatched, Gap: true);
                }

                return new FeedRunFailed(now, outcome.Error ?? "unknown error", failures);

            default:
                return null;
        }
    }

    public static FeedIngestStatusResult ToStatus(this FeedIngestState state, bool enabled, bool running) =>
        new(enabled, running, state.Cursor, state.LastRunAt, state.LastOutcome, state.LastNewItems,
            state.LastPublished, state.LastUnmatched, state.LastGap, state.ConsecutiveFailures);

    public static PersistedFeedIngestState GetPersistenceState(this FeedIngestState state) =>
        new(state.Cursor, state.LastRunAt, state.LastOutcome, state.LastNewItems, state.LastPublished,
            state.LastUnmatched, state.LastGap, state.ConsecutiveFailures);
}
