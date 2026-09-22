using FunkArr.Messages.Download;

namespace FunkArr.Download;

public readonly record struct QueueEntry(Guid Id, DownloadPriority Priority);
