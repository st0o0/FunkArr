namespace FunkArr.Persistence.Events.Download;

public sealed record HistoryRecorded(
    Guid DownloadId,
    string Title,
    string Category,
    long Size,
    int Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
