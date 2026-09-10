namespace FunkArr.Api.Models;

public sealed record DownloadQueueResponse(
    DownloadQueueItem[] Items,
    int TotalSlots,
    int ActiveCount,
    int QueuedCount);

public sealed record DownloadQueueItem(
    string DownloadId,
    string Title,
    string Status,
    string Channel,
    string Category,
    bool HasSubtitles,
    int TotalDuration,
    long TotalBytes,
    long BytesDownloaded,
    int Percentage,
    long Speed,
    string Eta);
