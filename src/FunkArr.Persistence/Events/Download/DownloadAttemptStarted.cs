namespace FunkArr.Persistence.Events.Download;

public sealed record DownloadAttemptStarted(Guid DownloadId, int Attempt);
