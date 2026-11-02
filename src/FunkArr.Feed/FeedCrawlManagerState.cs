using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed;

public sealed record FeedCrawlManagerState(
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

    public static readonly FeedCrawlManagerState Empty = new(0, null, null, 0, 0, 0, false, 0);

    public static FeedCrawlManagerState FromPersistence(PersistedFeedCrawlState persisted) =>
        new(persisted.Cursor, persisted.LastRunAt, persisted.LastOutcome, persisted.LastNewItems,
            persisted.LastPublished, persisted.LastUnmatched, persisted.LastGap, persisted.ConsecutiveFailures);
}

public static class FeedCrawlManagerStateExtensions
{
    public static FeedCrawlManagerState Apply(this FeedCrawlManagerState state, FeedCursorAdvanced evt) =>
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

    public static FeedCrawlManagerState Apply(this FeedCrawlManagerState state, FeedRunFailed evt) =>
        state with
        {
            LastRunAt = evt.FailedAt,
            LastOutcome = $"failed: {evt.Reason}",
            ConsecutiveFailures = evt.ConsecutiveFailures
        };

    public static FeedCrawlManagerState WithUnchangedRun(this FeedCrawlManagerState state, DateTimeOffset at) =>
        state with { LastRunAt = at, LastOutcome = "unchanged" };

    public static FeedCrawlStatusResult ToStatus(this FeedCrawlManagerState state, bool enabled, bool running) =>
        new(enabled, running, state.Cursor, state.LastRunAt, state.LastOutcome, state.LastNewItems,
            state.LastPublished, state.LastUnmatched, state.LastGap, state.ConsecutiveFailures);

    public static PersistedFeedCrawlState GetPersistenceState(this FeedCrawlManagerState state) =>
        new(state.Cursor, state.LastRunAt, state.LastOutcome, state.LastNewItems, state.LastPublished,
            state.LastUnmatched, state.LastGap, state.ConsecutiveFailures);
}
