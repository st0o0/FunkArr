namespace FunkArr.Api.Models;

public sealed record DownloadQueueResponse(
    DownloadQueueItem[] Items,
    int TotalSlots);

public sealed record DownloadQueueItem(
    string DownloadId,
    string Title,
    string Status,
    string Category,
    long TotalBytes,
    long BytesDownloaded,
    int Percentage,
    long Speed,
    string Eta);
