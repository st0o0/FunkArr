namespace FunkArr.DownloadClient.Queue;

public static class QueueActorEvents
{
    public sealed record JobEnqueued(
        string NzoId, string DownloadUrl, string Title, string? SubtitleUrl, string? Category, DateTimeOffset EnqueuedAt);

    public sealed record JobStarted(string NzoId);

    public sealed record JobFinished(string NzoId, string Outcome);

    public sealed record JobRemoved(string NzoId);
}
