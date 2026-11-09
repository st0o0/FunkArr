namespace FunkArr.Messages.Feed;

public sealed record QueryFeedCrawlStatus;

public abstract record FeedCrawlStatusResponse;

public sealed record FeedCrawlStatusResult(
    bool Enabled,
    bool Running,
    long Cursor,
    DateTimeOffset? LastRunAt,
    CrawlOutcome LastOutcome,
    string? LastError,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures) : FeedCrawlStatusResponse;

public sealed record FeedCrawlStatusFailed(Exception Cause) : FeedCrawlStatusResponse;
