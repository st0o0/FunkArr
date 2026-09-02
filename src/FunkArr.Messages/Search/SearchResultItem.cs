namespace FunkArr.Messages.Search;

public sealed record SearchResultItem(
    string Title,
    string Channel,
    string Topic,
    string Url,
    int Duration,
    long Size,
    int Quality,
    DateTimeOffset? AiredAt,
    double Score,
    int? TvdbId = null,
    string? ImdbId = null,
    int? TmdbId = null);
