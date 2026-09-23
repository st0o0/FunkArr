namespace FunkArr.Core;

public static class ReleaseTitleBuilder
{
    private static readonly char[] _invalidChars = ['/', ':', ';', '"', '\'', '@', '#', '?', '$', '%', '^', '*', '+', '=', '!', '<', '>', ',', '(', ')', '&'];

    public static string Format(string mediaName, string? identifier, string episodeTitle, int quality)
    {
        var parts = new List<string> { Sanitize(mediaName) };

        if (identifier is not null)
        {
            parts.Add(identifier);
        }

        parts.Add(Sanitize(episodeTitle));
        parts.Add("GERMAN");
        parts.Add(MapQuality(quality));
        parts.Add("WEB.h264-FunkArr");

        return CollapseDots(string.Join('.', parts));
    }

    public static string FormatSeasonEpisode(string season, string episode) =>
        $"S{PadNumber(season)}E{PadNumber(episode)}";

    public static string PadNumber(string value) =>
        value.Length < 2 ? value.PadLeft(2, '0') : value;

    internal static string MapQuality(int quality) => quality switch
    {
        1080 => "1080p",
        720 => "720p",
        480 => "480p",
        270 => "270p",
        _ => $"{quality}p",
    };

    internal static string Sanitize(string input)
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

    internal static string CollapseDots(string input)
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
