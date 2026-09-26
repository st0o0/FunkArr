namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadPhaseChanged(Guid DownloadId, PersistedDownloadPhase Phase);
