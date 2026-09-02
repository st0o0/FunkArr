namespace FunkArr.Messages.Download;

public sealed record HistoryResult(HistoryItem[] Items) : IDownloadResponse;

public sealed record HistoryItem(
    Guid DownloadId,
    string Title,
    string Category,
    long TotalBytes,
    int DownloadTimeSeconds,
    string FilePath,
    DownloadStatus Status,
    string FailMessage,
    long CompletedAt);
