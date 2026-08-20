namespace FunkArr.DownloadClient;

public static class DownloadEvents
{
    public sealed record DownloadEnqueued(
        string NzoId, string DownloadUrl, string Title, string? SubtitleUrl, DateTimeOffset EnqueuedAt);

    public sealed record DownloadStarted(string NzoId);

    public sealed record DownloadProgressUpdated(string NzoId, long DownloadedBytes, long TotalBytes);

    public sealed record DownloadCompleted(string NzoId, string TempFilePath, string? TempSubtitlePath);

    public sealed record DownloadFailed(string NzoId, string Error);

    public sealed record MuxingStarted(string NzoId);

    public sealed record MuxingCompleted(string NzoId, string OutputPath);

    public sealed record MuxingFailed(string NzoId, string Error);
}
