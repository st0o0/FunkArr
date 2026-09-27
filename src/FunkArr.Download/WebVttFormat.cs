using System.Globalization;
using System.Text.RegularExpressions;

namespace FunkArr.Download;

internal sealed partial class WebVttFormat : ISubtitleFormat
{
    public string Name => "WebVTT";

    public bool CanParse(string content) =>
        content.TrimStart().StartsWith("WEBVTT", StringComparison.Ordinal);

    public List<SubtitleCue> Parse(string content)
    {
        var cues = new List<SubtitleCue>();
        var lines = content.Split('\n');
        var i = 0;

        while (i < lines.Length && !lines[i].TrimStart().StartsWith("WEBVTT", StringComparison.Ordinal))
            i++;
        i++;

        while (i < lines.Length)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("NOTE", StringComparison.Ordinal) ||
                line.StartsWith("STYLE", StringComparison.Ordinal))
            {
                i++;
                while (i < lines.Length && lines[i].Trim().Length > 0)
                    i++;
                continue;
            }

            var match = TimestampLine().Match(line);
            if (!match.Success)
            {
                i++;
                continue;
            }

            var start = ParseVttTimestamp(match.Groups[1].Value);
            var end = ParseVttTimestamp(match.Groups[2].Value);
            i++;

            var textLines = new List<string>();
            while (i < lines.Length && lines[i].Trim().Length > 0)
            {
                textLines.Add(StripTags(lines[i].Trim()));
                i++;
            }

            var text = string.Join('\n', textLines).Trim();
            if (text.Length > 0)
                cues.Add(new SubtitleCue(start, end, text));
        }

        return cues;
    }

    private static TimeSpan ParseVttTimestamp(string value)
    {
        var parts = value.Split(':');
        if (parts.Length == 2 &&
            TimeSpan.TryParseExact(value, @"mm\:ss\.FFF", CultureInfo.InvariantCulture, out var ts2))
            return ts2;

        if (TimeSpan.TryParseExact(value, @"hh\:mm\:ss\.FFF", CultureInfo.InvariantCulture, out var ts))
            return ts;

        return TimeSpan.Zero;
    }

    private static string StripTags(string text) => TagPattern().Replace(text, "");

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"(\d{1,2}:\d{2}:\d{2}\.\d{3}|\d{2}:\d{2}\.\d{3})\s*-->\s*(\d{1,2}:\d{2}:\d{2}\.\d{3}|\d{2}:\d{2}\.\d{3})")]
    private static partial Regex TimestampLine();
}
