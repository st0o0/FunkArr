namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadSucceeded(
    Guid DownloadId,
    string FilePath,
    int DownloadTimeSeconds,
    long CompletedAt);
