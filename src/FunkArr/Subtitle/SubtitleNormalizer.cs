using System.Text;
using System.Text.RegularExpressions;

namespace FunkArr.Subtitle;

public static partial class SubtitleNormalizer
{
    public static async Task<string?> NormalizeAsync(string subtitlePath, string outputPath)
    {
        var format = await SubtitleFormatDetector.DetectFromFileAsync(subtitlePath);

        if (format == SubtitleFormat.Srt)
        {
            if (subtitlePath != outputPath)
            {
                File.Copy(subtitlePath, outputPath, overwrite: true);
            }

            return outputPath;
        }

        var content = await File.ReadAllTextAsync(subtitlePath);

        var srtContent = format switch
        {
            SubtitleFormat.WebVtt => ConvertVttToSrt(content),
            SubtitleFormat.Ttml => ConvertTtmlToSrt(content),
            _ => content,
        };

        await File.WriteAllTextAsync(outputPath, srtContent);
        return outputPath;
    }

    internal static string ConvertVttToSrt(string vttContent)
    {
        var lines = vttContent.Split('\n');
        var sb = new StringBuilder();
        var counter = 1;
        var inCue = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith("WEBVTT") || line.StartsWith("NOTE") || line.StartsWith("STYLE"))
            {
                continue;
            }

            if (line.Contains("-->"))
            {
                sb.AppendLine(counter.ToString());
                sb.AppendLine(line.Replace('.', ','));
                inCue = true;
                continue;
            }

            if (inCue && string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine();
                counter++;
                inCue = false;
                continue;
            }

            if (inCue)
            {
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    internal static string ConvertTtmlToSrt(string ttmlContent)
    {
        var sb = new StringBuilder();
        var counter = 1;

        foreach (Match match in TtmlParagraphPattern().Matches(ttmlContent))
        {
            var begin = NormalizeTtmlTimestamp(match.Groups[1].Value);
            var end = NormalizeTtmlTimestamp(match.Groups[2].Value);
            var text = Regex.Replace(match.Groups[3].Value, @"<[^>]+>", "").Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            sb.AppendLine(counter.ToString());
            sb.AppendLine($"{begin} --> {end}");
            sb.AppendLine(text);
            sb.AppendLine();
            counter++;
        }

        return sb.ToString();
    }

    internal static string NormalizeTtmlTimestamp(string ts)
    {
        if (ts.Contains('.'))
        {
            return ts.Replace('.', ',');
        }

        if (!ts.Contains(','))
        {
            return ts + ",000";
        }

        return ts;
    }

    [GeneratedRegex("""<p[^>]*\sbegin="([^"]+)"[^>]*\send="([^"]+)"[^>]*>(.*?)</p>""", RegexOptions.Singleline)]
    private static partial Regex TtmlParagraphPattern();
}
