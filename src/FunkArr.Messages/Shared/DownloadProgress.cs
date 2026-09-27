namespace FunkArr.Messages.Shared;

public sealed record DownloadProgress(
    long BytesDownloaded,
    long CurrentTimeUs,
    double Speed);
