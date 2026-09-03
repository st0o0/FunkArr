namespace FunkArr.Messages.Download;

public sealed record RecordDownload(
    Guid DownloadId,
    string Title,
    string Category,
    long Size,
    DownloadStatus Status,
    string? FilePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
