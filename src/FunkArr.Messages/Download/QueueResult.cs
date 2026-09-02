namespace FunkArr.Messages.Download;

public sealed record QueueResult(QueueItem[] Items, int TotalSlots) : IDownloadResponse;

public sealed record QueueItem(
    Guid DownloadId,
    string Title,
    DownloadStatus Status,
    long TotalBytes,
    long BytesDownloaded,
    long CurrentTimeUs,
    int TotalDuration,
    double Speed,
    string Category);
