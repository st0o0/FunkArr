namespace FunkArr.Messages.Download;

public sealed record StartDownload(Guid DownloadId) : IWithDownloadId;
