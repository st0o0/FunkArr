namespace FunkArr.Persistence.Events.Shared;

public sealed record PersistedDownloadMedia(
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    PersistedMediaType Category);
