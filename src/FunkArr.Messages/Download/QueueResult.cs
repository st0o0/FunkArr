using FunkArr.Messages.Shared;

namespace FunkArr.Messages.Download;

public abstract record QueueResponse;

public sealed record QueueResult(
    QueueItem[] Items,
    int TotalSlots,
    int TotalItems,
    bool IsPaused,
    bool IsScheduleActive,
    DateTimeOffset? NextWindow) : QueueResponse;

public sealed record QueueFailed(Exception Cause) : QueueResponse;

public sealed record QueueItem(
    Guid DownloadId,
    string Title,
    DownloadStatus Status,
    string Channel,
    bool HasSubtitles,
    long TotalBytes,
    DownloadProgress Progress,
    int TotalDuration,
    MediaType Category,
    DownloadPriority Priority,
    DownloadPhase Phase,
    int Attempt);
