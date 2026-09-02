namespace FunkArr.Messages.Download;

public sealed record DownloadFailed(
    Guid DownloadId,
    string Reason) : IWithDownloadId;
