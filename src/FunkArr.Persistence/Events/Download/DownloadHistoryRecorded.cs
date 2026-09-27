using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadHistoryRecorded(
    Guid DownloadId,
    PersistedDownloadCompletion Completion);
