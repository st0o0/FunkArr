using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadInitialized(
    Guid DownloadId,
    PersistedDownloadMedia Media);
