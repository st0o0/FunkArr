namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadInitialized(
    Guid DownloadId,
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    string Category);
