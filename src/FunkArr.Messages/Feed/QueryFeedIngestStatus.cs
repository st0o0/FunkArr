namespace FunkArr.Messages.Feed;

public sealed record QueryFeedIngestStatus;

public abstract record FeedIngestStatusResponse;

public sealed record FeedIngestStatusResult(
    bool Enabled,
    bool Running,
    long Cursor,
    DateTimeOffset? LastRunAt,
    string? LastOutcome,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures) : FeedIngestStatusResponse;

public sealed record FeedIngestStatusFailed(Exception Cause) : FeedIngestStatusResponse;
