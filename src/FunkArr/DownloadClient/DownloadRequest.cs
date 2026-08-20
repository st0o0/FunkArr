namespace FunkArr.DownloadClient;

public sealed record DownloadRequest(
    string NzoId,
    string VideoUrl,
    string? SubtitleUrl,
    string TempPath,
    string OutputDir,
    string Title);
