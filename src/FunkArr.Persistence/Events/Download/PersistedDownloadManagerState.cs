namespace FunkArr.Persistence.Events.Download;

public sealed record PersistedDownloadManagerState(
    PersistedQueueEntry[] Queued,
    PersistedDispatchedEntry[] Dispatched,
    bool Paused);

public sealed record PersistedQueueEntry(Guid DownloadId, PersistedDownloadPriority Priority, PersistedMediaType Category = PersistedMediaType.Show);

public sealed record PersistedDispatchedEntry(Guid DownloadId, PersistedDownloadPriority Priority, PersistedMediaType Category = PersistedMediaType.Show);
