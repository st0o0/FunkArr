namespace FunkArr.Persistence.Events.Shared;

public sealed record PersistedDownloadCompletion(
    string Title,
    PersistedMediaType Category,
    long Size,
    PersistedDownloadStatus Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
