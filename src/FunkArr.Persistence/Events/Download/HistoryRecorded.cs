namespace FunkArr.Persistence.Events.Download;

public sealed record HistoryRecorded(
    Guid DownloadId,
    string Title,
    PersistedMediaType Category,
    long Size,
    PersistedDownloadStatus Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
