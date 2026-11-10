using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Models.Downloads;

public enum QueueStatus
{
    Unknown = 0,
    Processing = 1,
    Queued = 2
}

public enum QueuePhase
{
    Unknown = 0,
    VideoDownload = 1,
    Remuxing = 2,
    Moving = 3
}

public enum QueuePriority
{
    Unknown = 0,
    Low = 2,
    Normal = 3,
    High = 4
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
