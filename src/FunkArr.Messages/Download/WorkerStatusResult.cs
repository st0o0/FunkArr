using FunkArr.Messages.Shared;

namespace FunkArr.Messages.Download;

public sealed record WorkerStatusResult(
    Guid DownloadId,
    string Title,
    MediaType Category,
    string Channel,
    bool HasSubtitles,
    long Size,
    WorkerStatus Status,
    DownloadProgress Progress,
    int TotalDuration,
    string? FailMessage,
    DownloadPhase Phase,
    int Attempt);
