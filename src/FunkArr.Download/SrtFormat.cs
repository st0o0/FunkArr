using System.Globalization;
using System.Text.RegularExpressions;

namespace FunkArr.Download;

internal sealed partial class SrtFormat : ISubtitleFormat
{
    public string Name => "SRT";

    public bool CanParse(string content)
    {
        var trimmed = content.TrimStart();
        return trimmed.Length > 5 && char.IsDigit(trimmed[0]) && trimmed.Contains("-->");
    }

    public List<SubtitleCue> Parse(string content)
    {
        var cues = new List<SubtitleCue>();
        var lines = content.Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            while (i < lines.Length && lines[i].Trim().Length == 0)
                i++;

            if (i >= lines.Length)
                break;

            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || !char.IsDigit(trimmed[0]))
            {
                i++;
                continue;
            }
            i++;

            if (i >= lines.Length)
                break;

            var match = TimestampLine().Match(lines[i].Trim());
            if (!match.Success)
                continue;

            var start = ParseSrtTimestamp(match.Groups[1].Value);
            var end = ParseSrtTimestamp(match.Groups[2].Value);
            i++;

            var textLines = new List<string>();
            while (i < lines.Length && lines[i].Trim().Length > 0)
            {
                textLines.Add(lines[i].Trim());
                i++;
            }

            var text = string.Join('\n', textLines).Trim();
            if (text.Length > 0)
                cues.Add(new SubtitleCue(start, end, text));
        }

        return cues;
    }

    private static TimeSpan ParseSrtTimestamp(string value)
    {
        var normalized = value.Replace(',', '.');
        if (TimeSpan.TryParseExact(normalized, @"hh\:mm\:ss\.FFF", CultureInfo.InvariantCulture, out var ts))
            return ts;

        if (TimeSpan.TryParse(normalized, CultureInfo.InvariantCulture, out ts))
            return ts;

        return TimeSpan.Zero;
    }

    [GeneratedRegex(@"(\d{2}:\d{2}:\d{2}[,\.]\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2}[,\.]\d{3})")]
    private static partial Regex TimestampLine();
}
