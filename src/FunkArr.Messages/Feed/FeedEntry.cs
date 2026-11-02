using FunkArr.Messages.Search;

namespace FunkArr.Messages.Feed;

public sealed record FeedEntry(
    string StableId,
    DateTimeOffset FirstSeenAt,
    MediaType MediaType,
    SearchResultItem Item);
