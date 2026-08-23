namespace FunkArr.DownloadClient;

public enum DownloadSourceType
{
    Direct,
    Hls,
}

public static class DownloadSourceDetector
{
    public static DownloadSourceType Detect(string url)
    {
        if (url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
            || url.Contains(".m3u8?", StringComparison.OrdinalIgnoreCase))
        {
            return DownloadSourceType.Hls;
        }

        return DownloadSourceType.Direct;
    }

    public static int PartitionOutlet(DownloadRequest request) =>
        Detect(request.VideoUrl) == DownloadSourceType.Hls ? 0 : 1;
}
