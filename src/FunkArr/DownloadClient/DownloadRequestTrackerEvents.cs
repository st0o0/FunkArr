namespace FunkArr.DownloadClient;

public static class DownloadRequestTrackerEvents
{
    public sealed record RequestCreated(
        string NzoId, string Title, string DownloadUrl, DateTimeOffset EnqueuedAt);

    public sealed record StatusChanged(string NzoId, string Status);

    public sealed record Completed(string NzoId, string OutputPath, DateTimeOffset CompletedAt);

    public sealed record Failed(string NzoId, string Error, DateTimeOffset CompletedAt);
}
