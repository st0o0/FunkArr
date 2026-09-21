using FunkArr.Messages;

namespace FunkArr.Messages.Download;

public abstract record QueueResponse;

public sealed record QueueResult(QueueItem[] Items, int TotalSlots, int TotalItems) : QueueResponse;

public sealed record QueueFailed(Exception Cause) : QueueResponse;

public sealed record QueueItem(
    Guid DownloadId,
    string Title,
    DownloadStatus Status,
    string Channel,
    bool HasSubtitles,
    long TotalBytes,
    long BytesDownloaded,
    long CurrentTimeUs,
    int TotalDuration,
    double Speed,
    MediaType Category);
