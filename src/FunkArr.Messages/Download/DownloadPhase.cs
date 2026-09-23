namespace FunkArr.Messages.Download;

public enum DownloadPhase
{
    Downloading = 0,
    Remuxing = 1,
}

public static class DownloadPhaseExtensions
{
    public static DownloadPhase DerivePhase(long bytesDownloaded, long totalBytes, long currentTimeUs) =>
        bytesDownloaded >= totalBytes && currentTimeUs > 0
            ? DownloadPhase.Remuxing
            : DownloadPhase.Downloading;

    public static int CalculatePercentage(this DownloadPhase phase, long bytesDownloaded, long totalBytes, long currentTimeUs, int totalDuration) => phase switch
    {
        DownloadPhase.Downloading => totalBytes > 0
            ? Math.Clamp((int)(bytesDownloaded * 100 / totalBytes), 0, 100)
            : 0,
        DownloadPhase.Remuxing => totalDuration > 0
            ? Math.Clamp((int)(currentTimeUs / 1_000_000.0 / totalDuration * 100), 0, 100)
            : 0,
        _ => 0,
    };
}
