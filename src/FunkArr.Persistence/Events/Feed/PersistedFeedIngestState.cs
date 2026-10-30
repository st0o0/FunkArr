namespace FunkArr.Persistence.Events.Feed;

public sealed record PersistedFeedIngestState(
    long Cursor,
    DateTimeOffset? LastRunAt,
    string? LastOutcome,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures);
