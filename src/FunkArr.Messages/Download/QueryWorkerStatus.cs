namespace FunkArr.Messages.Download;

public sealed record QueryWorkerStatus(Guid DownloadId) : IWithDownloadId;
