namespace FunkArr.Messages.Feed;

public sealed record AddFeedEntries(FeedEntry[] Entries);

public abstract record AddFeedEntriesResponse;

public sealed record AddFeedEntriesCompleted(int Added) : AddFeedEntriesResponse;

public sealed record AddFeedEntriesFailed(Exception Cause) : AddFeedEntriesResponse;
