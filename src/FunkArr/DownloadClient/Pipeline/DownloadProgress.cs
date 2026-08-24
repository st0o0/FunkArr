using System.Threading;

namespace FunkArr.DownloadClient.Pipeline;

public sealed class DownloadProgress
{
    private long _downloadedBytes;
    private long _totalBytes;

    public long DownloadedBytes
    {
        get => Volatile.Read(ref _downloadedBytes);
        set => Volatile.Write(ref _downloadedBytes, value);
    }

    public long TotalBytes
    {
        get => Volatile.Read(ref _totalBytes);
        set => Volatile.Write(ref _totalBytes, value);
    }

    public double Percent => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
}
