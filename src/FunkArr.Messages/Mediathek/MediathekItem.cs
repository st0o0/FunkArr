namespace FunkArr.Messages.Mediathek;

public sealed record MediathekItem(
    string Channel,
    string Topic,
    string Title,
    string? Description,
    long Timestamp,
    int Duration,
    long Size,
    string? UrlVideoLow,
    string? UrlVideo,
    string? UrlVideoHd,
    string? UrlSubtitle,
    string? UrlWebsite);
