namespace FunkArr.Messages.Download;

public sealed record WorkerStatusResult(
    Guid DownloadId,
    string Title,
    string Category,
    long Size,
    int Status,
    long BytesDownloaded,
    long CurrentTimeUs,
    int TotalDuration,
    double Speed,
    string? FilePath,
    string? FailMessage);
