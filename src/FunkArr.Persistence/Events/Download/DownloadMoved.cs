namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadMoved(Guid DownloadId, int Position);
