namespace FunkArr.DownloadClient.Ffmpeg;

public interface IFfmpegService
{
    Task DownloadHlsAsync(string nzoId, string url, CancellationToken ct = default);
    Task<bool> HasSubtitleStreamAsync(string manifestUrl, CancellationToken ct = default);
    Task<bool> ExtractSubtitleAsync(string nzoId, string manifestUrl, CancellationToken ct = default);
    Task<string> RemuxAsync(string nzoId, string title, bool hasSubtitle, string? category = null, CancellationToken ct = default);
}
