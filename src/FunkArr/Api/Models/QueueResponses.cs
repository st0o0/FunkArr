namespace FunkArr.Api.Models;

public sealed record QueueItemResponse(
    string NzoId,
    string Title,
    string Status,
    double ProgressPercent,
    long DownloadedBytes,
    long TotalBytes,
    DateTimeOffset EnqueuedAt);

public sealed record HistoryItemResponse(
    string NzoId,
    string Title,
    string Status,
    string OutputPath,
    string? ErrorMessage,
    DateTimeOffset EnqueuedAt,
    DateTimeOffset? CompletedAt);
