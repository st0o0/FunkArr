namespace FunkArr.Api.Models.Downloads;

public sealed record HistoryStatsResponse(
    int TotalCompleted,
    int TotalFailed,
    long TotalBytes,
    int AverageDownloadTimeSeconds,
    double SuccessRate);
