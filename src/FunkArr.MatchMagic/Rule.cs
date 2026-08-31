using System.Globalization;
using System.Text.RegularExpressions;

namespace FunkArr.MatchMagic;

public sealed record Rule(
    string Id,
    int Priority,
    float? Confidence,
    MatchStrategy Strategy,
    FilterGroup Filters,
    string? SeasonRegex = null,
    string? EpisodeRegex = null,
    int? CaptureGroup = null,
    IReadOnlyList<TitleRule>? TitleRules = null)
{
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly string[] _germanMonths =
    [
        "Januar", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember"
    ];

    public MatchResult? Match(MediaItem item, float defaultConfidence)
    {
        if (!Filters.Evaluate(item))
        {
            return null;
        }

        var identification = Identify(item);
        if (identification is null)
        {
            return null;
        }

        var qualities = BuildQualityVariants(item);
        var confidence = Confidence ?? defaultConfidence;

        return new MatchResult(item, this, identification, BuildConstructedTitle(item), confidence, qualities);
    }

    private EpisodeIdentification? Identify(MediaItem item) => Strategy switch
    {
        MatchStrategy.SeasonAndEpisodeNumber => IdentifySeasonEpisode(item),
        MatchStrategy.ItemTitleExact => IdentifyTitleExact(item),
        MatchStrategy.ItemTitleIncludes => IdentifyTitleIncludes(item),
        MatchStrategy.ItemTitleEqualsAirdate => IdentifyAirdate(item),
        MatchStrategy.ByAbsoluteEpisodeNumber => IdentifyAbsoluteEpisode(item),
        _ => null,
    };

    private EpisodeIdentification? IdentifySeasonEpisode(MediaItem item)
    {
        if (SeasonRegex is null || EpisodeRegex is null)
        {
            return null;
        }

        var season = ExtractCapture(item.Title, SeasonRegex);
        var episode = ExtractCapture(item.Title, EpisodeRegex);

        if (season is null || episode is null)
        {
            return null;
        }

        return new EpisodeIdentification(Season: season, Episode: episode);
    }

    private EpisodeIdentification? IdentifyTitleExact(MediaItem item)
    {
        var title = BuildConstructedTitle(item);
        return title is not null ? new EpisodeIdentification(Title: title) : null;
    }

    private EpisodeIdentification? IdentifyTitleIncludes(MediaItem item)
    {
        var title = BuildConstructedTitle(item);
        if (title is null)
        {
            return null;
        }

        var normalizedTitle = NormalizeUmlauts(title);
        var normalizedItemTitle = NormalizeUmlauts(item.Title);

        return normalizedItemTitle.Contains(normalizedTitle, StringComparison.OrdinalIgnoreCase)
            ? new EpisodeIdentification(Title: title)
            : null;
    }

    private static EpisodeIdentification? IdentifyAirdate(MediaItem item)
    {
        var date = ExtractGermanDate(item.Title);
        return date is not null
            ? new EpisodeIdentification(Title: date.Value.ToString("yyyy-MM-dd"))
            : null;
    }

    private EpisodeIdentification? IdentifyAbsoluteEpisode(MediaItem item)
    {
        if (EpisodeRegex is null)
        {
            return null;
        }

        var episode = ExtractCapture(item.Title, EpisodeRegex);
        return episode is not null ? new EpisodeIdentification(Episode: episode) : null;
    }

    private string? BuildConstructedTitle(MediaItem item)
    {
        if (TitleRules is not { Count: > 0 })
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var rule in TitleRules)
        {
            if (rule.Type == "static")
            {
                parts.Add(rule.Value ?? "");
            }
            else if (rule.Type == "regex" && rule.Pattern is not null)
            {
                var fieldValue = ResolveTitleRuleField(item, rule.Field);
                if (fieldValue is null)
                {
                    return null;
                }

                var captured = ExtractCapture(fieldValue, rule.Pattern, rule.CaptureGroup);
                if (captured is null)
                {
                    return null;
                }

                parts.Add(captured);
            }
            else
            {
                return null;
            }
        }

        return string.Concat(parts);
    }

    private static string? ResolveTitleRuleField(MediaItem item, string? field) => field switch
    {
        "title" => item.Title,
        "description" => item.Description,
        "topic" => item.Topic,
        _ => item.Title,
    };

    private string? ExtractCapture(string input, string pattern, int? groupOverride = null)
    {
        var group = groupOverride ?? CaptureGroup;

        try
        {
            var match = Regex.Match(input, pattern, RegexOptions.None, _regexTimeout);
            if (!match.Success)
            {
                return null;
            }

            var groupIndex = group ?? (match.Groups.Count - 1);
            if (groupIndex < 0 || groupIndex >= match.Groups.Count)
            {
                return null;
            }

            var captured = match.Groups[groupIndex].Value;
            return string.IsNullOrEmpty(captured) ? null : captured;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static DateTime? ExtractGermanDate(string text)
    {
        var numericMatch = Regex.Match(text, @"(\d{1,2})\.(\d{1,2})\.(\d{4}|\d{2})", RegexOptions.None, _regexTimeout);
        if (numericMatch.Success)
        {
            var day = int.Parse(numericMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(numericMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var year = int.Parse(numericMatch.Groups[3].Value, CultureInfo.InvariantCulture);

            if (year < 100)
            {
                year += 2000;
            }

            try
            {
                return new DateTime(year, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        var germanMonthPattern = @"(\d{1,2})\.\s*(\w+)\s+(\d{4})";
        var longMatch = Regex.Match(text, germanMonthPattern, RegexOptions.None, _regexTimeout);
        if (longMatch.Success)
        {
            var day = int.Parse(longMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var monthName = longMatch.Groups[2].Value;
            var year = int.Parse(longMatch.Groups[3].Value, CultureInfo.InvariantCulture);

            var monthIndex = Array.FindIndex(_germanMonths, m =>
                string.Equals(m, monthName, StringComparison.OrdinalIgnoreCase));

            if (monthIndex >= 0)
            {
                try
                {
                    return new DateTime(year, monthIndex + 1, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<QualityVariant> BuildQualityVariants(MediaItem item)
    {
        var variants = new List<QualityVariant>(3);

        if (!string.IsNullOrEmpty(item.UrlVideoHd))
        {
            variants.Add(new QualityVariant(Quality.HD1080, item.UrlVideoHd, EstimateSize(item.Duration, 5_000)));
        }

        if (!string.IsNullOrEmpty(item.UrlVideo))
        {
            variants.Add(new QualityVariant(Quality.HD720, item.UrlVideo, EstimateSize(item.Duration, 2_500)));
        }

        if (!string.IsNullOrEmpty(item.UrlVideoLow))
        {
            variants.Add(new QualityVariant(Quality.SD, item.UrlVideoLow, EstimateSize(item.Duration, 800)));
        }

        return variants;
    }

    private static long EstimateSize(int durationSeconds, int bitrateKbps) =>
        (long)durationSeconds * bitrateKbps * 1000 / 8;

    private static string NormalizeUmlauts(string input) => input
        .Replace("ä", "ae", StringComparison.Ordinal)
        .Replace("ö", "oe", StringComparison.Ordinal)
        .Replace("ü", "ue", StringComparison.Ordinal)
        .Replace("Ä", "Ae", StringComparison.Ordinal)
        .Replace("Ö", "Oe", StringComparison.Ordinal)
        .Replace("Ü", "Ue", StringComparison.Ordinal)
        .Replace("ß", "ss", StringComparison.Ordinal);
}
