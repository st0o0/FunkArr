namespace FunkArr.Messages.Download;

public sealed record SetDownloadPriority(Guid DownloadId, DownloadPriority Priority) : IWithDownloadId;

public abstract record SetDownloadPriorityResponse;

public sealed record SetDownloadPriorityCompleted : SetDownloadPriorityResponse;

public sealed record SetDownloadPriorityFailed(string Reason) : SetDownloadPriorityResponse;
