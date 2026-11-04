namespace FunkArr.Download.Ffmpeg;

internal sealed record FfmpegInput(
    string VideoUrl, string? SubtitlePath, string OutputPath,
    string? ProxyUrl, string? SubtitleLanguage, bool IsHls);
