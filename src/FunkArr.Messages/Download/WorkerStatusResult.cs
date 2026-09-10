namespace FunkArr.Messages.Download;

public sealed record WorkerStatusResult(
    Guid DownloadId,
    string Title,
    string Category,
    string Channel,
    bool HasSubtitles,
    long Size,
    int Status,
    long BytesDownloaded,
    long CurrentTimeUs,
    int TotalDuration,
    double Speed,
    string? FailMessage);
