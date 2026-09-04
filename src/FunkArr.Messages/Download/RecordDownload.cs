namespace FunkArr.Messages.Download;

public sealed record RecordDownload(
    Guid DownloadId,
    string Title,
    string Category,
    long Size,
    DownloadStatus Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
