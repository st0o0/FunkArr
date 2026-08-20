namespace FunkArr.DownloadClient;

public abstract record DownloadOutcome(string NzoId)
{
    public sealed record Success(string NzoId, string VideoPath, string? SubtitlePath) : DownloadOutcome(NzoId);
    public sealed record Failure(string NzoId, string Reason) : DownloadOutcome(NzoId);
}
