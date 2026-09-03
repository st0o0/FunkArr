namespace FunkArr.Messages.Download;

public sealed record DeleteDownload(Guid DownloadId, bool DeleteFiles = false) : IWithDownloadId;

public sealed record DeleteDownloadResult(bool Success, string? Error);
