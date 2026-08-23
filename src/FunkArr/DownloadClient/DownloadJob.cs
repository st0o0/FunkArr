namespace FunkArr.DownloadClient;

public sealed record DownloadJob
{
    public required string NzoId { get; init; }
    public required string DownloadUrl { get; init; }
    public required string Title { get; init; }
    public string? SubtitleUrl { get; init; }
    public DownloadStatus Status { get; init; } = DownloadStatus.Queued;
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset EnqueuedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

public enum DownloadStatus
{
    Queued,
    Downloading,
    Muxing,
    Completed,
    Failed,
}
