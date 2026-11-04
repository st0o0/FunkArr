using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Models.Downloads;

public enum HistoryStatus
{
    Unknown = 0,
    Completed = 1,
    Failed = 2
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
