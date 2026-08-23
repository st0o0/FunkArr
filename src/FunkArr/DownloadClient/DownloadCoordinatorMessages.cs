namespace FunkArr.DownloadClient;

public sealed record StartDownload(
    string NzoId, string VideoUrl, string? SubtitleUrl,
    string TempPath, string OutputDir, string Title) : IWithNzoId;

public sealed record CancelDownload(string NzoId) : IWithNzoId;

internal sealed record VideoFetchDone(string NzoId, string VideoPath);
internal sealed record SubtitleAcquireDone(string NzoId, string? SubtitlePath);
internal sealed record NoSubtitleAvailable(string NzoId);
internal sealed record SubtitleConvertDone(string NzoId, string NormalizedPath);
internal sealed record RemuxDone(string NzoId, string OutputPath);
internal sealed record WorkerFailed(string NzoId, FailureKind Kind, string Reason);
