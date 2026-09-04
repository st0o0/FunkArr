namespace FunkArr.Messages.Download;

public sealed record HistoryResult(HistoryItem[] Items, int TotalItems);

public sealed record HistoryItem(
    Guid DownloadId,
    string Title,
    string Category,
    long TotalBytes,
    int DownloadTimeSeconds,
    string RelativePath,
    DownloadStatus Status,
    string FailMessage,
    long CompletedAt);
