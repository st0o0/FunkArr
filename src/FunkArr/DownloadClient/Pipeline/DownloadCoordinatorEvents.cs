namespace FunkArr.DownloadClient.Pipeline;

public static class DownloadActorStageEvents
{
    public sealed record JobAccepted(
        string NzoId, string VideoUrl, string? SubtitleUrl, string Title, string? Category);

    public sealed record StageEntered(string NzoId, string Stage);

    public sealed record JobCompleted(string NzoId, string OutputPath);

    public sealed record JobFailed(string NzoId, string FailureKind, string Reason);

    public sealed record JobCancelled(string NzoId);
}
