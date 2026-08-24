using FunkArr.DownloadClient.Tracker;

namespace FunkArr.DownloadClient.Pipeline;

public sealed record StartDownload(
    string NzoId, string VideoUrl, string? SubtitleUrl, string Title, string? Category = null) : IWithNzoId;

public sealed record CancelDownload(string NzoId) : IWithNzoId;

internal sealed record FetchVideo(string NzoId, string Url) : IWithNzoId;
internal sealed record AcquireSubtitle(string NzoId, string? SubtitleUrl, string? HlsManifestUrl) : IWithNzoId;
internal sealed record ConvertSubtitle(string NzoId) : IWithNzoId;
internal sealed record RemuxVideo(string NzoId, string Title, bool HasSubtitle, string? Category = null) : IWithNzoId;

internal sealed record VideoFetched(string NzoId);
internal sealed record SubtitleAcquired(string NzoId, bool Found);
internal sealed record SubtitleConverted(string NzoId);
internal sealed record VideoRemuxed(string NzoId);
internal sealed record WorkerFailed(string NzoId, FailureKind Kind, string Reason);
