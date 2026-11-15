namespace FunkArr.Messages.Download;

public sealed record RetryDownload(Guid DownloadId, MediaType? Category = null) : IWithDownloadId;

public sealed record RetryDownloadResult(bool Success, string? Error);
