using FunkArr.Messages;

namespace FunkArr.Persistence.Events.Download;

public sealed record HistoryRecorded(
    Guid DownloadId,
    string Title,
    MediaType Category,
    long Size,
    int Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
