namespace FunkArr.Messages.Feed;

public sealed record QueryFeed(
    MediaType? MediaType, int Offset, int Limit,
    string? Channel = null, int? Quality = null, string? Search = null);

public abstract record FeedResponse;

public sealed record FeedResult(FeedEntry[] Entries, int Total, int FilteredTotal) : FeedResponse;

public sealed record FeedFailed(Exception Cause) : FeedResponse;
