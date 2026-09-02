namespace FunkArr.Messages.Download;

public sealed record StartDownload(
    Guid DownloadId,
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    string OutputPath) : IWithDownloadId;
