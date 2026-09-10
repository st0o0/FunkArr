namespace FunkArr.Api.Models;

public sealed record HistoryStatsResponse(
    int TotalCompleted,
    int TotalFailed,
    long TotalBytes,
    int AverageDownloadTimeSeconds,
    double SuccessRate);
