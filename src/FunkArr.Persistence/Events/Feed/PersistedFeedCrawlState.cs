namespace FunkArr.Persistence.Events.Feed;

public sealed record PersistedFeedCrawlState(
    long Cursor,
    DateTimeOffset? LastRunAt,
    PersistedCrawlOutcome LastOutcome,
    string? LastError,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures);
