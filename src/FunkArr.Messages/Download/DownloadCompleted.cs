namespace FunkArr.Messages.Download;

public sealed record DownloadCompleted(
    Guid DownloadId,
    string FilePath,
    int DownloadTimeSeconds) : IWithDownloadId;
