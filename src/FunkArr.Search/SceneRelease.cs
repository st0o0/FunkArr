using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Search;

public sealed record SceneRelease(
    string MediaName,
    string? Identifier,
    string EpisodeTitle,
    EnrichedItem Item)
{
    public static SceneRelease ForShow(EnrichedItem item, string? mediaName)
    {
        var display = ResolveDisplay(item, mediaName);
        var si = item.Identity as ShowIdentity;
        var identifier = (si?.Season, si?.Episode) switch
        {
            ({ } s, { } e) => $"S{PadNumber(s)}E{PadNumber(e)}",
            (null, { } e) => $"S01E{PadNumber(e)}",
            _ => item.Source.AiredAt?.ToString("yyyy-MM-dd"),
        };
        return new SceneRelease(display.MediaName, identifier, display.EpisodeTitle, item);
    }

    public static SceneRelease ForMovie(EnrichedItem item, string? mediaName)
    {
        var display = ResolveDisplay(item, mediaName);
        var mi = item.Identity as MovieIdentity;
        var year = mi?.Year?.ToString()
                ?? item.Source.AiredAt?.Year.ToString();
        return new SceneRelease(display.MediaName, year, display.EpisodeTitle, item);
    }

    public string FormatTitle(int quality)
    {
        var parts = new List<string> { Sanitize(MediaName) };

        if (Identifier is not null)
        {
            parts.Add(Identifier);
        }

        parts.Add(Sanitize(EpisodeTitle));
        parts.Add("GERMAN");
        parts.Add(MapQuality(quality));
        parts.Add("WEB.h264-FunkArr");

        return CollapseDots(string.Join('.', parts));
    }

    public SearchResultItem[] Expand()
    {
        var variants = VideoQuality.GetVariants(Item.Source);
        return
        [
            .. variants.Select(v => new SearchResultItem(
                Title: FormatTitle(v.Quality),
                Channel: Item.Source.Channel,
                Topic: Item.Source.Topic,
                Url: v.Url,
                Duration: Item.Source.Duration,
                Size: Item.Source.Size > 0 ? Item.Source.Size : v.EstimatedSize,
                Quality: v.Quality,
                AiredAt: Item.Source.AiredAt,
                Score: Item.Score,
                SubtitleUrl: Item.Source.SubtitleUrl,
                Metadata: BuildMetadata(Item)))
        ];
    }

    private static MatchMetadata BuildMetadata(EnrichedItem item)
    {
        var (ids, season, episode) = item.Identity switch
        {
            ShowIdentity si => (new ExternalIds(si.TvdbId, si.ImdbId, null), si.Season, si.Episode),
            MovieIdentity mi => (new ExternalIds(null, mi.ImdbId, mi.TmdbId), (string?)null, (string?)null),
            _ => (new ExternalIds(null, item.Identity.ImdbId, null), (string?)null, (string?)null),
        };
        return new MatchMetadata(ids, season, episode, item.Match?.Confidence, item.Match?.Method);
    }

    private static ReleaseDisplay ResolveDisplay(EnrichedItem item, string? mediaName)
    {
        if (item.Display is not null)
        {
            return item.Display;
        }

        var name = mediaName ?? item.Source.Topic;
        return ReleaseDisplay.From(item.Source.Title, name);
    }

    private static string PadNumber(string value) =>
        value.Length < 2 ? value.PadLeft(2, '0') : value;

    private static readonly char[] _invalidChars =
        ['/', ':', ';', '"', '\'', '@', '#', '?', '$', '%', '^', '*', '+', '=', '!', '<', '>', ',', '(', ')', '&'];

    private static string Sanitize(string input)
    {
        var chars = new char[input.Length];
        var pos = 0;

        foreach (var c in input)
        {
            if (Array.IndexOf(_invalidChars, c) >= 0)
            {
                continue;
            }

            chars[pos++] = c == ' ' ? '.' : c;
        }

        return new string(chars, 0, pos);
    }

    private static string MapQuality(int quality) => quality switch
    {
        1080 => "1080p",
        720 => "720p",
        480 => "480p",
        270 => "270p",
        _ => $"{quality}p",
    };

    private static string CollapseDots(string input)
    {
        var result = new char[input.Length];
        var pos = 0;
        var prevDot = false;

        foreach (var c in input)
        {
            if (c == '.')
            {
                if (!prevDot)
                {
                    result[pos++] = c;
                }

                prevDot = true;
            }
            else
            {
                result[pos++] = c;
                prevDot = false;
            }
        }

        var start = pos > 0 && result[0] == '.' ? 1 : 0;
        var end = pos > 0 && result[pos - 1] == '.' ? pos - 1 : pos;

        return new string(result, start, end - start);
    }
}
