namespace FunkArr.Api.Models;

public sealed record DownloadHistoryResponse(
    DownloadHistoryItem[] Items,
    int TotalItems);

public sealed record DownloadHistoryItem(
    string DownloadId,
    string Title,
    string Category,
    long TotalBytes,
    int DownloadTimeSeconds,
    string? RelativePath,
    string Status,
    string? FailMessage,
    string CompletedAt);
