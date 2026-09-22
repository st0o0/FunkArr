namespace FunkArr.Messages.Download;

public sealed record MoveDownload(Guid DownloadId, int Position, DownloadPriority? Priority = null) : IWithDownloadId;

public abstract record MoveDownloadResponse;

public sealed record MoveDownloadCompleted : MoveDownloadResponse;

public sealed record MoveDownloadFailed(string Reason) : MoveDownloadResponse;
