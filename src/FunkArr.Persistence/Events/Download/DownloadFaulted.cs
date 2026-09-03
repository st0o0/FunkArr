namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadFaulted(Guid DownloadId, string Reason);
