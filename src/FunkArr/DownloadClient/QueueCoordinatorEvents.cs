namespace FunkArr.DownloadClient;

public static class QueueCoordinatorEvents
{
    public sealed record JobEnqueued(
        string NzoId, string DownloadUrl, string Title, string? SubtitleUrl, DateTimeOffset EnqueuedAt);

    public sealed record JobStarted(string NzoId);

    public sealed record JobFinished(string NzoId, string Outcome);

    public sealed record JobRemoved(string NzoId);
}
