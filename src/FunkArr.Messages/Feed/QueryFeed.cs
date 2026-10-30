namespace FunkArr.Messages.Feed;

public sealed record QueryFeed(MediaType? MediaType, int Offset, int Limit);

public abstract record FeedResponse;

public sealed record FeedResult(FeedEntry[] Entries, int Total) : FeedResponse;

public sealed record FeedFailed(Exception Cause) : FeedResponse;
