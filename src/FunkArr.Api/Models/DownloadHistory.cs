namespace FunkArr.Api.Models;

public enum HistoryStatus
{
    Completed,
    Failed,
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
