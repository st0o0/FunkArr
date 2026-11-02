using FunkArr.Messages.Shared;

namespace FunkArr.Messages.Feed;

public sealed record FeedEntry(
    string StableId,
    DateTimeOffset FirstSeenAt,
    MediaType MediaType,
    string Title,
    string Channel,
    string Topic,
    string Url,
    int Duration,
    long Size,
    int Quality,
    DateTimeOffset? AiredAt,
    double Score,
    string? SubtitleUrl,
    ExternalIds? Ids,
    string? Season,
    string? Episode);
