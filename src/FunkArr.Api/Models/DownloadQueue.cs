using System.ComponentModel.DataAnnotations;

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
    string Phase,
    long Speed,
    string Eta,
    string Priority);

public sealed record MoveRequest([property: Range(0, int.MaxValue)] int Position, string? Priority = null);

public sealed record PriorityRequest([property: Required] string Priority);

public sealed record SwapRequest(Guid Id1, Guid Id2);
