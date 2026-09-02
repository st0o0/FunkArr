namespace FunkArr.Messages.Download;

public sealed record DownloadProgress(
    Guid DownloadId,
    long CurrentTimeUs,
    int TotalDuration,
    long BytesDownloaded,
    long TotalBytes,
    double Speed) : IWithDownloadId;
