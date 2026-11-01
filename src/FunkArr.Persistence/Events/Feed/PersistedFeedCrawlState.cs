namespace FunkArr.Persistence.Events.Feed;

public sealed record PersistedFeedCrawlState(
    long Cursor,
    DateTimeOffset? LastRunAt,
    string? LastOutcome,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures);
