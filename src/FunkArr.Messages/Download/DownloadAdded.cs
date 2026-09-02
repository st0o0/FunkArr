namespace FunkArr.Messages.Download;

public sealed record DownloadAdded(Guid DownloadId) : IWithDownloadId, IDownloadResponse;
