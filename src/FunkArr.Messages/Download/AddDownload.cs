namespace FunkArr.Messages.Download;

public sealed record AddDownload(
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    MediaType Category);
