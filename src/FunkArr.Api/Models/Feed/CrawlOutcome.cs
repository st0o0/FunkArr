namespace FunkArr.Api.Models.Feed;

public enum CrawlOutcome
{
    Unknown = 0,
    Completed = 1,
    CompletedWithGap = 2,
    Unchanged = 3,
    Failed = 4
}
