namespace FunkArr.Messages.Download;

public enum DownloadPhase
{
    Initialized = 0,
    VideoDownload = 1,
    Remuxing = 2,
    Moving = 3,
    Completed = 4,
    Failed = 5
}

public static class DownloadPhaseExtensions
{
    public static DownloadPhase DerivePhase(long bytesDownloaded, long totalBytes, long currentTimeUs) =>
        bytesDownloaded >= totalBytes && currentTimeUs > 0
            ? DownloadPhase.Remuxing
            : DownloadPhase.VideoDownload;

    public static int CalculatePercentage(this DownloadPhase phase, long bytesDownloaded, long totalBytes, long currentTimeUs, int totalDuration) => phase switch
    {
        DownloadPhase.VideoDownload => totalBytes > 0
            ? Math.Clamp((int)(bytesDownloaded * 100 / totalBytes), 0, 100)
            : 0,
        DownloadPhase.Remuxing => totalDuration > 0
            ? Math.Clamp((int)(currentTimeUs / 1_000_000.0 / totalDuration * 100), 0, 100)
            : 0,
        _ => 0
    };

    public static bool IsTransient(this DownloadPhase phase) =>
        phase is DownloadPhase.VideoDownload or DownloadPhase.Remuxing or DownloadPhase.Moving;
}
