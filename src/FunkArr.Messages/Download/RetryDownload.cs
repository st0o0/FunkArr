namespace FunkArr.Messages.Download;

public sealed record RetryDownload(Guid DownloadId) : IWithDownloadId;

public sealed record RetryDownloadResult(bool Success, string? Error) : IDownloadResponse;
