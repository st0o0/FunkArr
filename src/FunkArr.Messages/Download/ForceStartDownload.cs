namespace FunkArr.Messages.Download;

public sealed record ForceStartDownload(Guid DownloadId) : IWithDownloadId;

public sealed record ForceStartDownloadResult(bool Success, string? Error);
