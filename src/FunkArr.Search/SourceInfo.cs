using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

public sealed record SourceInfo(
    string Channel,
    string Topic,
    string Title,
    string? Description,
    int Duration,
    long Size,
    DateTimeOffset? AiredAt,
    string? UrlHd,
    string? Url,
    string? UrlLow,
    string? SubtitleUrl)
{
    public static SourceInfo From(MediathekItem item) => new(
        item.Channel,
        item.Topic,
        item.Title,
        item.Description,
        item.Duration,
        item.Size,
        item.Timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(item.Timestamp) : null,
        item.UrlVideoHd,
        item.UrlVideo,
        item.UrlVideoLow,
        item.UrlSubtitle);

    public int ResolveQuality() =>
        UrlHd is not null ? 1080 :
        Url is not null ? 720 :
        UrlLow is not null ? 480 : 0;
}
