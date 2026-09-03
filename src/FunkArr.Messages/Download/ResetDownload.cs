namespace FunkArr.Messages.Download;

public sealed record ResetDownload(Guid DownloadId) : IWithDownloadId;
