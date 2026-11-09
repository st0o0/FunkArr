using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Crawl;

public sealed class FeedCrawlManagerState
{
    internal const int MaxConsecutiveFailures = 3;

    public long Cursor { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public CrawlOutcome LastOutcome { get; set; }
    public string? LastError { get; set; }
    public int LastNewItems { get; set; }
    public int LastPublished { get; set; }
    public int LastUnmatched { get; set; }
    public bool LastGap { get; set; }
    public int ConsecutiveFailures { get; set; }
    public bool Running { get; set; }
    public bool ResultReceived { get; set; }

    public static FeedCrawlManagerState Empty => new();

    public static FeedCrawlManagerState FromPersistence(PersistedFeedCrawlState persisted) =>
        new()
        {
            Cursor = persisted.Cursor,
            LastRunAt = persisted.LastRunAt,
            LastOutcome = persisted.LastOutcome.ToDomain(),
            LastError = persisted.LastError,
            LastNewItems = persisted.LastNewItems,
            LastPublished = persisted.LastPublished,
            LastUnmatched = persisted.LastUnmatched,
            LastGap = persisted.LastGap,
            ConsecutiveFailures = persisted.ConsecutiveFailures
        };

    public void Apply(FeedCursorAdvanced evt)
    {
        Cursor = evt.Cursor;
        LastRunAt = evt.CompletedAt;
        LastOutcome = evt.Gap ? CrawlOutcome.CompletedWithGap : CrawlOutcome.Completed;
        LastError = null;
        LastNewItems = evt.NewItems;
        LastPublished = evt.Published;
        LastUnmatched = evt.Unmatched;
        LastGap = evt.Gap;
        ConsecutiveFailures = 0;
    }

    public void Apply(FeedRunFailed evt)
    {
        LastRunAt = evt.FailedAt;
        LastOutcome = CrawlOutcome.Failed;
        LastError = evt.Reason;
        ConsecutiveFailures = evt.ConsecutiveFailures;
    }

    public void WithUnchangedRun(DateTimeOffset at)
    {
        LastRunAt = at;
        LastOutcome = CrawlOutcome.Unchanged;
        LastError = null;
    }

    public FeedCrawlStatusResult ToStatus(bool enabled, bool running) =>
        new(enabled, running, Cursor, LastRunAt, LastOutcome, LastError, LastNewItems,
            LastPublished, LastUnmatched, LastGap, ConsecutiveFailures);

    public PersistedFeedCrawlState GetPersistenceState() =>
        new(Cursor, LastRunAt, LastOutcome.ToPersistence(), LastError, LastNewItems,
            LastPublished, LastUnmatched, LastGap, ConsecutiveFailures);
}
