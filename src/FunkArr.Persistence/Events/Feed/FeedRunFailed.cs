namespace FunkArr.Persistence.Events.Feed;

public sealed record FeedRunFailed(
    DateTimeOffset FailedAt,
    string Reason,
    int ConsecutiveFailures);
