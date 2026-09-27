namespace FunkArr.Messages.Shared;

public sealed record DownloadMedia(
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    MediaType Category);
