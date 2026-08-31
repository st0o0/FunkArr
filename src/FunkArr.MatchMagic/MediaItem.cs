namespace FunkArr.MatchMagic;

public sealed record MediaItem(
    string Topic,
    string Title,
    string? Description,
    string Channel,
    long Timestamp,
    int Duration,
    string? UrlVideoHd,
    string? UrlVideo,
    string? UrlVideoLow,
    string? UrlSubtitle = null,
    string? UrlWebsite = null,
    long Size = 0);
