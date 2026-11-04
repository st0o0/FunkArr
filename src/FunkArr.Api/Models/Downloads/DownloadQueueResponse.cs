namespace FunkArr.Api.Models;

public enum QueueStatus
{
    Processing,
    Queued
}

public enum QueuePhase
{
    VideoDownload,
    Remuxing,
    Moving
}

public enum QueuePriority
{
    Low = -1,
    Normal = 0,
    High = 1
}

public sealed record DownloadQueueResponse(
    DownloadQueueItem[] Items,
    int TotalSlots,
    int ActiveCount,
    int QueuedCount,
    bool IsPaused,
    bool IsScheduleActive,
    DateTimeOffset? NextWindow);

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
    QueuePhase Phase,
    long Speed,
    string Eta,
    QueuePriority Priority);
