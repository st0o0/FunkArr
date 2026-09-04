namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadSucceeded(
    Guid DownloadId,
    int DownloadTimeSeconds,
    long CompletedAt);
