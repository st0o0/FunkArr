using FunkArr.Messages.Shared;

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
    string? SubtitleUrl = null,
    MatchMetadata? Metadata = null);
