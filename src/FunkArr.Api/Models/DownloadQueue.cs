namespace FunkArr.Api.Models;

public enum QueueStatus
{
    Processing,
    Queued,
}

public sealed record DownloadQueueResponse(
    DownloadQueueItem[] Items,
    int TotalSlots,
    int ActiveCount,
    int QueuedCount);

public sealed record DownloadQueueItem(
    string DownloadId,
    string Title,
    QueueStatus Status,
    string Channel,
    MediaType Category,
    bool HasSubtitles,
    int TotalDuration,
    long TotalBytes,
    long BytesDownloaded,
    int Percentage,
    long Speed,
    string Eta);
