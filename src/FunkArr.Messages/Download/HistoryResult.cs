using FunkArr.Messages.Shared;

namespace FunkArr.Messages.Download;

public sealed record HistoryResult(HistoryItem[] Items, int TotalItems);

public sealed record HistoryItem(
    Guid DownloadId,
    DownloadCompletion Completion);
