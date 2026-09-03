namespace FunkArr.Messages.Download;

public sealed record CancelDownload(Guid DownloadId) : IWithDownloadId;
