using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Models.Downloads;

public enum HistoryStatus
{
    Completed,
    Failed
}

public sealed record DownloadHistoryResponse(
    DownloadHistoryItem[] Items,
    int TotalItems);

public sealed record DownloadHistoryItem(
    string DownloadId,
    string Title,
    MediaType Category,
    long TotalBytes,
    int DownloadTimeSeconds,
    string? RelativePath,
    HistoryStatus Status,
    string? FailMessage,
    DateTimeOffset CompletedAt);
