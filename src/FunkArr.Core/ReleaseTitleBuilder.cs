using FunkArr.Messages.Scoring;

namespace FunkArr.Core;

public static class ReleaseTitleBuilder
{
    private static readonly char[] _invalidChars = ['/', ':', ';', '"', '\'', '@', '#', '?', '$', '%', '^', '*', '+', '=', '!', '<', '>', ',', '(', ')', '&'];

    public static string Build(string topic, string title, MetadataSpec? metadata, int quality, string category)
    {
        var parts = new List<string> { Sanitize(topic) };

        if (category == "movie")
        {
            AppendMovieIdentifier(parts, metadata);
        }
        else
        {
            AppendTvIdentifier(parts, metadata);
        }

        parts.Add(Sanitize(title));
        parts.Add("GERMAN");
        parts.Add(MapQuality(quality));
        parts.Add("WEB.h264-FunkArr");

        return CollapseDots(string.Join('.', parts));
    }

    private static void AppendTvIdentifier(List<string> parts, MetadataSpec? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        if (metadata.Season is not null && metadata.Episode is not null)
        {
            parts.Add(FormatSeasonEpisode(metadata.Season, metadata.Episode));
        }
        else if (metadata.Episode is not null)
        {
            parts.Add($"E{PadNumber(metadata.Episode)}");
        }
        else if (metadata.AiredAt is not null)
        {
            parts.Add(metadata.AiredAt.Value.ToString("yyyy-MM-dd"));
        }
    }

    private static void AppendMovieIdentifier(List<string> parts, MetadataSpec? metadata)
    {
        if (metadata?.AiredAt is not null)
        {
            parts.Add(metadata.AiredAt.Value.Year.ToString());
        }
    }

    private static string FormatSeasonEpisode(string season, string episode) =>
        $"S{PadNumber(season)}E{PadNumber(episode)}";

    private static string PadNumber(string value) =>
        value.Length < 2 ? value.PadLeft(2, '0') : value;

    private static string MapQuality(int quality) => quality switch
    {
        1080 => "1080p",
        720 => "720p",
        480 => "480p",
        270 => "270p",
        _ => $"{quality}p",
    };

    private static string Sanitize(string input)
    {
        var normalized = NormalizeUmlauts(input);

        var chars = new char[normalized.Length];
        var pos = 0;

        foreach (var c in normalized)
        {
            if (Array.IndexOf(_invalidChars, c) >= 0)
            {
                continue;
            }

            chars[pos++] = c == ' ' ? '.' : c;
        }

        return new string(chars, 0, pos);
    }

    private static string NormalizeUmlauts(string input) => input
        .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
        .Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue")
        .Replace("ß", "ss");

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
