namespace FunkArr.Messages.Download;

public sealed record QuerySingleHistoryItem(Guid DownloadId);

public sealed record SingleHistoryItemResult(HistoryItem? Item);
