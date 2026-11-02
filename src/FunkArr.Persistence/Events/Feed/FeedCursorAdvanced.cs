namespace FunkArr.Persistence.Events.Feed;

public sealed record FeedCursorAdvanced(
    long Cursor,
    DateTimeOffset CompletedAt,
    int NewItems,
    int Published,
    int Unmatched,
    bool Gap);
