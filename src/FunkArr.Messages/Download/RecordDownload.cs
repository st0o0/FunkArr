namespace FunkArr.Messages.Download;

public sealed record RecordDownload(
    Guid DownloadId,
    string Title,
    MediaType Category,
    long Size,
    DownloadStatus Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
