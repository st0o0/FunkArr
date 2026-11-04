using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed;

public sealed class FeedCrawlManagerState
{
    internal const int MaxConsecutiveFailures = 3;

    public long Cursor { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public string? LastOutcome { get; set; }
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
            LastOutcome = persisted.LastOutcome,
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
        LastOutcome = evt.Gap ? "completed_with_gap" : "completed";
        LastNewItems = evt.NewItems;
        LastPublished = evt.Published;
        LastUnmatched = evt.Unmatched;
        LastGap = evt.Gap;
        ConsecutiveFailures = 0;
    }

    public void Apply(FeedRunFailed evt)
    {
        LastRunAt = evt.FailedAt;
        LastOutcome = $"failed: {evt.Reason}";
        ConsecutiveFailures = evt.ConsecutiveFailures;
    }

    public void WithUnchangedRun(DateTimeOffset at)
    {
        LastRunAt = at;
        LastOutcome = "unchanged";
    }

    public FeedCrawlStatusResult ToStatus(bool enabled, bool running) =>
        new(enabled, running, Cursor, LastRunAt, LastOutcome, LastNewItems,
            LastPublished, LastUnmatched, LastGap, ConsecutiveFailures);

    public PersistedFeedCrawlState GetPersistenceState() =>
        new(Cursor, LastRunAt, LastOutcome, LastNewItems, LastPublished,
            LastUnmatched, LastGap, ConsecutiveFailures);
}
