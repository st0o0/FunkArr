namespace FunkArr.Messages.Download;

public sealed record SwapDownloads(Guid DownloadId1, Guid DownloadId2);

public abstract record SwapDownloadsResponse;

public sealed record SwapDownloadsCompleted : SwapDownloadsResponse;

public sealed record SwapDownloadsFailed(string Reason) : SwapDownloadsResponse;
