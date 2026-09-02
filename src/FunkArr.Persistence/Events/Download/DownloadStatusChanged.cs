namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadStatusChanged(
    Guid DownloadId,
    int Status,
    string? FilePath,
    int DownloadTimeSeconds,
    string? FailMessage,
    long CompletedAt);
