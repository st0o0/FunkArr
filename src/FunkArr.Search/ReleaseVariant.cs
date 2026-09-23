using System.Text.RegularExpressions;
using FunkArr.Core;
using FunkArr.Messages;
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

        var display = item.Display ?? new ReleaseDisplay(
            mediaName ?? item.Source.Topic,
            CleanTitle(item.Source.Title, mediaName ?? item.Source.Topic));

        var identifier = BuildIdentifier(item.Identity, item.Source.AiredAt, mediaType);

        return variants.Select(v => new ReleaseVariant(
            ReleaseTitleBuilder.Format(display.MediaName, identifier, display.EpisodeTitle, v.Quality),
            v.Url,
            item.Source,
            item.Identity,
            item.Score,
            v.Quality,
            item.Source.Size > 0 ? item.Source.Size : v.EstimatedSize,
            item.Match)).ToArray();
    }

    internal static string? BuildIdentifier(MediaIdentity identity, DateTimeOffset? airedAt, MediaType mediaType) =>
        (identity.Season, identity.Episode, airedAt, mediaType) switch
        {
            ({ } s, { } e, _, MediaType.Show) => ReleaseTitleBuilder.FormatSeasonEpisode(s, e),
            (null, { } e, _, MediaType.Show) => $"S01E{ReleaseTitleBuilder.PadNumber(e)}",
            (_, _, { } at, MediaType.Show) => at.ToString("yyyy-MM-dd"),
            (_, _, { } at, MediaType.Movie) => at.Year.ToString(),
            _ => null,
        };

    private static readonly Regex _seasonEpisodePattern = new(@"\(?\s*S\d{1,4}\s*/?\s*E\d{1,4}\s*\)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string CleanTitle(string title, string mediaName)
    {
        if (string.IsNullOrEmpty(mediaName) || string.IsNullOrEmpty(title))
        {
            return title;
        }

        var result = title;

        string[] prefixSeparators = [": ", ": ", " - ", " "];
        foreach (var sep in prefixSeparators)
        {
            var prefix = mediaName + sep;
            if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var stripped = result[prefix.Length..].Trim();
                if (stripped.Length > 0)
                {
                    result = stripped;
                }

                break;
            }
        }

        string[] suffixSeparators = [" - ", " – "];
        foreach (var sep in suffixSeparators)
        {
            var suffix = sep + mediaName;
            if (result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var stripped = result[..^suffix.Length].Trim();
                if (stripped.Length > 0)
                {
                    result = stripped;
                }

                break;
            }
        }

        result = _seasonEpisodePattern.Replace(result, "").Trim();

        return result;
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
