using System.Text.RegularExpressions;

namespace FunkArr.Search;

public sealed partial record ReleaseDisplay(string MediaName, string EpisodeTitle)
{
    public static ReleaseDisplay From(string sourceTitle, string mediaName)
    {
        var episodeTitle = Clean(sourceTitle, mediaName);
        return new ReleaseDisplay(mediaName, episodeTitle);
    }

    private static string Clean(string title, string mediaName)
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
            if (!result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var stripped = result[prefix.Length..].Trim();
            if (stripped.Length > 0)
            {
                result = stripped;
            }

            break;
        }

        string[] suffixSeparators = [" - ", " – "];
        foreach (var sep in suffixSeparators)
        {
            var suffix = sep + mediaName;
            if (!result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var stripped = result[..^suffix.Length].Trim();
            if (stripped.Length > 0)
            {
                result = stripped;
            }

            break;
        }

        result = SeasonEpisodePattern().Replace(result, "").Trim();

        return result;
    }

    [GeneratedRegex(@"\(?\s*S\d{1,4}\s*/?\s*E\d{1,4}\s*\)?", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonEpisodePattern();
}
