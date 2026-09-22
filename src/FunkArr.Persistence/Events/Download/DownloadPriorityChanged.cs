namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadPriorityChanged(Guid DownloadId, PersistedDownloadPriority Priority);
