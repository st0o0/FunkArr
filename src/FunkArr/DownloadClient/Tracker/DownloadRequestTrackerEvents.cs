namespace FunkArr.DownloadClient.Tracker;

public static class DownloadRequestActorEvents
{
    public sealed record RequestCreated(
        string NzoId, string Title, string DownloadUrl, string? Category, DateTimeOffset EnqueuedAt);

    public sealed record StatusChanged(string NzoId, string Status);

    public sealed record Completed(string NzoId, string OutputPath, DateTimeOffset CompletedAt);

    public sealed record Failed(string NzoId, string Error, DateTimeOffset CompletedAt);
}
