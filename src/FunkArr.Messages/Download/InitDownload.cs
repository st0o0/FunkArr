namespace FunkArr.Messages.Download;

public sealed record InitDownload(
    Guid DownloadId,
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    string Category) : IWithDownloadId;
