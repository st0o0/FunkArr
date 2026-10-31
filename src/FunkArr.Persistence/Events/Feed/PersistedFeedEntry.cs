namespace FunkArr.Persistence.Events.Feed;

public sealed record PersistedFeedEntry(
    string StableId,
    DateTimeOffset FirstSeenAt,
    PersistedMediaType MediaType,
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
    int? TvdbId,
    string? ImdbId,
    int? TmdbId,
    string? Season,
    string? Episode);
