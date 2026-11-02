namespace FunkArr.Messages.Feed;

public sealed record PublishFeedEntries(FeedEntry[] Entries);

public abstract record PublishFeedEntriesResponse;

public sealed record PublishFeedEntriesCompleted(int Published) : PublishFeedEntriesResponse;

public sealed record PublishFeedEntriesFailed(Exception Cause) : PublishFeedEntriesResponse;
