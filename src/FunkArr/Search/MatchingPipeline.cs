using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

public static partial class MatchingPipeline
{
    private static readonly string[] SkipKeywords =
    [
        "Audiodeskription",
        "Trailer",
        "Gebärdensprache",
        "Hörfassung",
        "Vorschau",
        "Teaser",
    ];

    public static async Task<IReadOnlyList<SearchResult>> ExecuteAsync(
        IEnumerable<MediathekResultItem> items,
        MatchContext context,
        QualityProbeService probeService,
        int probeLimit)
    {
        var matched = items
            .Where(i => !ShouldSkip(i))
            .Where(i => MatchesShow(i, context))
            .Where(i => MatchesEpisode(i, context))
            .Where(i => IsDurationAcceptable(i, context))
            .ToList();

        var results = new List<SearchResult>();
        foreach (var item in matched)
        {
            var variants = await probeService.ExpandWithProbingAsync(item, probeLimit, results.Count);
            results.AddRange(variants);
        }

        return results
            .Select(r => ScoreResult(r, context))
            .OrderByDescending(r => r.Score)
            .ToList();
    }

    public static IReadOnlyList<SearchResult> Execute(
        IEnumerable<MediathekResultItem> items,
        MatchContext context)
    {
        return items
            .Where(i => !ShouldSkip(i))
            .Where(i => MatchesShow(i, context))
            .Where(i => MatchesEpisode(i, context))
            .Where(i => IsDurationAcceptable(i, context))
            .SelectMany(ExpandQualities)
            .Select(r => ScoreResult(r, context))
            .OrderByDescending(r => r.Score)
            .ToList();
    }

    public static IReadOnlyList<SearchResult> FilterResults(
        IEnumerable<MediathekResultItem> items,
        int? expectedDurationSeconds = null,
        double durationThreshold = 0.35)
    {
        var results = new List<SearchResult>();

        foreach (var item in items)
        {
            if (ShouldSkip(item))
            {
                continue;
            }

            if (expectedDurationSeconds.HasValue &&
                !IsDurationAcceptable(item.Duration, expectedDurationSeconds.Value, durationThreshold))
            {
                continue;
            }

            AddQualityVariants(results, item);
        }

        return results;
    }

    internal static bool MatchesShow(MediathekResultItem item, MatchContext context)
    {
        if (context.ShowName is null)
        {
            return true;
        }

        return MatchesTitle(item.Topic, context.ShowName) ||
               MatchesTitle(item.Title, context.ShowName);
    }

    internal static bool MatchesEpisode(MediathekResultItem item, MatchContext context)
    {
        if (context.Season is null && context.Episode is null && context.AirDate is null)
        {
            return true;
        }

        if (context.Season is not null && context.Episode is not null)
        {
            var se = ExtractSeasonEpisode(item.Title);
            if (se is not null && se.Value.season == context.Season && se.Value.episode == context.Episode)
            {
                return true;
            }
        }

        if (context.AirDate is not null && DateMatcher.MatchesAirDate(item.Title, item.Description, context.AirDate.Value))
        {
            return true;
        }

        if (context.Season is not null && context.Episode is not null && context.AirDate is null)
        {
            return false;
        }

        return true;
    }

    internal static SearchResult ScoreResult(SearchResult result, MatchContext context)
    {
        var score = 0.0;

        score += result.Quality switch
        {
            QualityTier.HD1080 => 30,
            QualityTier.HD720 => 20,
            QualityTier.SD => 10,
            _ => 0,
        };

        if (context.ShowName is not null)
        {
            var normalizedTopic = NormalizeTitle(result.Topic);
            var normalizedShow = NormalizeTitle(context.ShowName);
            if (normalizedTopic.Equals(normalizedShow, StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }
            else if (normalizedTopic.Contains(normalizedShow, StringComparison.OrdinalIgnoreCase))
            {
                score += 30;
            }
        }

        if (context.AirDate is not null)
        {
            var daysDiff = Math.Abs((result.Timestamp - context.AirDate.Value).TotalDays);
            score += Math.Max(0, 20 - daysDiff);
        }

        return result with { Score = score };
    }

    public static string NormalizeTitle(string title)
    {
        var sb = new StringBuilder(title.Length);
        foreach (var c in title)
        {
            var mapped = c switch
            {
                'ä' or 'Ä' => "ae",
                'ö' or 'Ö' => "oe",
                'ü' or 'Ü' => "ue",
                'ß' => "ss",
                _ => null,
            };

            if (mapped is not null)
            {
                sb.Append(mapped);
            }
            else
            {
                sb.Append(char.ToLower(c, CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }

    public static (int season, int episode)? ExtractSeasonEpisode(string title)
    {
        var match = SeasonEpisodePattern().Match(title);
        if (!match.Success)
        {
            return null;
        }

        return (
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    public static bool MatchesTitle(string candidateTitle, string expectedTitle)
    {
        var normalizedCandidate = NormalizeTitle(candidateTitle);
        var normalizedExpected = NormalizeTitle(expectedTitle);

        if (normalizedCandidate.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedExpected.Length >= 13 &&
            normalizedCandidate.Contains(normalizedExpected[..13], StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldSkip(MediathekResultItem item)
    {
        foreach (var keyword in SkipKeywords)
        {
            if (item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.Topic.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDurationAcceptable(MediathekResultItem item, MatchContext context)
    {
        if (context.ExpectedDurationSeconds is null)
        {
            return true;
        }

        return IsDurationAcceptable(item.Duration, context.ExpectedDurationSeconds.Value, 0.35);
    }

    private static bool IsDurationAcceptable(int actualDuration, int expectedDuration, double threshold)
    {
        if (expectedDuration <= 0 || actualDuration <= 0)
        {
            return true;
        }

        var deviation = Math.Abs(actualDuration - expectedDuration) / (double)expectedDuration;
        return deviation <= threshold;
    }

    private static IEnumerable<SearchResult> ExpandQualities(MediathekResultItem item)
    {
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);

        if (!string.IsNullOrEmpty(item.Url_Video_HD))
        {
            yield return CreateResult(item, item.Url_Video_HD, QualityTier.HD1080, timestamp);
        }

        if (!string.IsNullOrEmpty(item.Url_Video))
        {
            yield return CreateResult(item, item.Url_Video, QualityTier.HD720, timestamp);
        }

        if (!string.IsNullOrEmpty(item.Url_Video_Low))
        {
            yield return CreateResult(item, item.Url_Video_Low, QualityTier.SD, timestamp);
        }
    }

    private static void AddQualityVariants(List<SearchResult> results, MediathekResultItem item)
    {
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);

        if (!string.IsNullOrEmpty(item.Url_Video_HD))
        {
            results.Add(CreateResult(item, item.Url_Video_HD, QualityTier.HD1080, timestamp));
        }

        if (!string.IsNullOrEmpty(item.Url_Video))
        {
            results.Add(CreateResult(item, item.Url_Video, QualityTier.HD720, timestamp));
        }

        if (!string.IsNullOrEmpty(item.Url_Video_Low))
        {
            results.Add(CreateResult(item, item.Url_Video_Low, QualityTier.SD, timestamp));
        }
    }

    private static SearchResult CreateResult(
        MediathekResultItem item, string url, QualityTier quality, DateTimeOffset timestamp) =>
        new()
        {
            Title = item.Title,
            Topic = item.Topic,
            Channel = item.Channel,
            Url = url,
            UrlSubtitle = string.IsNullOrEmpty(item.Url_Subtitle) ? null : item.Url_Subtitle,
            DurationSeconds = item.Duration,
            SizeBytes = item.Size > 0 ? item.Size : EstimateSize(item.Duration, quality),
            Timestamp = timestamp,
            Description = string.IsNullOrEmpty(item.Description) ? null : item.Description,
            Quality = quality,
        };

    private static long EstimateSize(int durationSeconds, QualityTier quality)
    {
        var bitrateKbps = quality switch
        {
            QualityTier.HD1080 => 4000,
            QualityTier.HD720 => 2000,
            _ => 800,
        };
        return (long)durationSeconds * bitrateKbps * 1000 / 8;
    }

    [GeneratedRegex(@"S(\d{2})E(\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonEpisodePattern();
}