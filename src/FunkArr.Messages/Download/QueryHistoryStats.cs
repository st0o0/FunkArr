namespace FunkArr.Messages.Download;

public sealed record QueryHistoryStats;

public sealed record HistoryStatsResult(
    int TotalCompleted,
    int TotalFailed,
    long TotalBytes,
    int AverageDownloadTimeSeconds,
    double SuccessRate);
