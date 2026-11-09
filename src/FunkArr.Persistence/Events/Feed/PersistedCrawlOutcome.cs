namespace FunkArr.Persistence.Events.Feed;

public enum PersistedCrawlOutcome
{
    Unknown = 0,
    Completed = 1,
    CompletedWithGap = 2,
    Unchanged = 3,
    Failed = 4
}
