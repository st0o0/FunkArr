using FunkArr.Messages;
using FunkArr.Messages.Download;

namespace FunkArr.Download;

public readonly record struct QueueEntry(Guid Id, DownloadPriority Priority, MediaType Category);

public readonly record struct DispatchedEntry(DownloadPriority Priority, MediaType Category);
