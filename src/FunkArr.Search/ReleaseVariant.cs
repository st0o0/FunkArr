using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record ReleaseVariant(
    string Title,
    string Url,
    SourceInfo Source,
    MediaIdentity Identity,
    double Score,
    int Quality,
    long Size,
    MatchInfo? Match)
{
    public static ReleaseVariant[] Expand(EnrichedItem item, MediaType mediaType, string? mediaName)
    {
        var variants = VideoQuality.GetVariants(item.Source);
        if (variants.Length == 0)
        {
            return [];
        }

        var metadata = new MetadataSpec(
            item.Identity.Season, item.Identity.Episode, item.Source.AiredAt);

        return variants.Select(v => new ReleaseVariant(
            ReleaseTitleBuilder.Build(
                mediaName ?? item.Source.Topic, item.Source.Title, metadata, v.Quality, mediaType),
            v.Url,
            item.Source,
            item.Identity,
            item.Score,
            v.Quality,
            item.Source.Size > 0 ? item.Source.Size : v.EstimatedSize,
            item.Match)).ToArray();
    }

    public SearchResultItem ToResultItem() => new(
        Title: Title,
        Channel: Source.Channel,
        Topic: Source.Topic,
        Url: Url,
        Duration: Source.Duration,
        Size: Size,
        Quality: Quality,
        AiredAt: Source.AiredAt,
        Score: Score,
        SubtitleUrl: Source.SubtitleUrl,
        TvdbId: Identity.TvdbId,
        ImdbId: Identity.ImdbId,
        TmdbId: Identity.TmdbId,
        Season: Identity.Season,
        Episode: Identity.Episode,
        MatchConfidence: Match?.Confidence,
        MatchMethod: Match?.Method);
}
