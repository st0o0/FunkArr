using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FunkArr.Search;

namespace FunkArr.RuleSet;

public static class RuleSetMatchingEngine
{
    private static readonly string[] AccessibilityKeywords =
    [
        "Audiodeskription",
        "Gebärdensprache",
        "Gebardensprache",
        "klare Sprache",
        "Hörfassung",
    ];

    public static MatchedEpisodeInfo? EvaluateRules(
        MediathekResultItem item,
        IReadOnlyList<Rule> rules,
        IReadOnlyList<TvdbEpisodeInfo> tvdbEpisodes,
        string showName)
    {
        if (ShouldSkipAccessibility(item))
        {
            return null;
        }

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            if (!EvaluateFilterGroup(item, rule.Filters))
            {
                continue;
            }

            var match = rule.Strategy switch
            {
                MatchingStrategy.SeasonAndEpisodeNumber =>
                    MatchSeasonAndEpisode(item, rule, tvdbEpisodes, showName),
                MatchingStrategy.ItemTitleExact =>
                    MatchTitleExact(item, rule, tvdbEpisodes, showName),
                MatchingStrategy.ItemTitleIncludes =>
                    MatchTitleIncludes(item, rule, tvdbEpisodes, showName),
                MatchingStrategy.ItemTitleEqualsAirdate =>
                    MatchAirdate(item, rule, tvdbEpisodes, showName),
                MatchingStrategy.ByAbsoluteEpisodeNumber =>
                    MatchAbsoluteEpisode(item, rule, tvdbEpisodes, showName),
                _ => null,
            };

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public static (IReadOnlyList<MatchedEpisodeInfo> Matches, IReadOnlyList<MatchTrace> Traces)
        EvaluateRulesWithTraces(
            IReadOnlyList<MediathekResultItem> items,
            IReadOnlyList<Rule> rules,
            IReadOnlyList<TvdbEpisodeInfo> tvdbEpisodes,
            string showName,
            double fileConfidence = 1.0)
    {
        var matches = new List<MatchedEpisodeInfo>();
        var traces = new List<MatchTrace>();
        var orderedRules = rules.OrderBy(r => r.Priority).ToList();

        foreach (var item in items)
        {
            var baseTrace = new { item.Title, item.Topic, item.Duration, item.Channel };

            if (ShouldSkipAccessibility(item))
            {
                traces.Add(new FilteredTrace
                {
                    ItemTitle = item.Title,
                    ItemTopic = item.Topic,
                    ItemDuration = item.Duration,
                    ItemChannel = item.Channel,
                    FilterField = "title",
                    FilterOp = "contains",
                    FilterValue = "accessibility-keyword",
                    ActualValue = item.Title,
                    Reason = "accessibility-skip",
                });
                continue;
            }

            var ruleFailures = new List<RuleFailure>();
            MatchedEpisodeInfo? matchResult = null;
            int matchedRuleIndex = -1;

            for (var i = 0; i < orderedRules.Count; i++)
            {
                var rule = orderedRules[i];

                if (!EvaluateFilterGroup(item, rule.Filters))
                {
                    ruleFailures.Add(new RuleFailure
                    {
                        RuleIndex = i,
                        FailReason = "filter-failed",
                        Detail = FindFailingFilter(item, rule.Filters),
                    });
                    continue;
                }

                var match = rule.Strategy switch
                {
                    MatchingStrategy.SeasonAndEpisodeNumber =>
                        MatchSeasonAndEpisode(item, rule, tvdbEpisodes, showName),
                    MatchingStrategy.ItemTitleExact =>
                        MatchTitleExact(item, rule, tvdbEpisodes, showName),
                    MatchingStrategy.ItemTitleIncludes =>
                        MatchTitleIncludes(item, rule, tvdbEpisodes, showName),
                    MatchingStrategy.ItemTitleEqualsAirdate =>
                        MatchAirdate(item, rule, tvdbEpisodes, showName),
                    MatchingStrategy.ByAbsoluteEpisodeNumber =>
                        MatchAbsoluteEpisode(item, rule, tvdbEpisodes, showName),
                    _ => null,
                };

                if (match is not null)
                {
                    matchResult = match;
                    matchedRuleIndex = i;
                    break;
                }

                ruleFailures.Add(new RuleFailure
                {
                    RuleIndex = i,
                    FailReason = "strategy-no-match",
                    Detail = $"strategy={rule.Strategy}",
                });
            }

            if (matchResult is not null)
            {
                var matchedRule = orderedRules[matchedRuleIndex];
                matches.Add(matchResult);
                traces.Add(new MatchedTrace
                {
                    ItemTitle = item.Title,
                    ItemTopic = item.Topic,
                    ItemDuration = item.Duration,
                    ItemChannel = item.Channel,
                    RuleIndex = matchedRuleIndex,
                    Strategy = matchedRule.Strategy,
                    Confidence = matchedRule.Confidence ?? fileConfidence,
                    Season = matchResult.Episode.AiredSeason,
                    Episode = matchResult.Episode.AiredEpisodeNumber,
                    EpisodeName = matchResult.Episode.EpisodeName,
                });
            }
            else
            {
                traces.Add(new UnmatchedTrace
                {
                    ItemTitle = item.Title,
                    ItemTopic = item.Topic,
                    ItemDuration = item.Duration,
                    ItemChannel = item.Channel,
                    RuleFailures = ruleFailures,
                });
            }
        }

        return (matches, traces);
    }

    private static string? FindFailingFilter(MediathekResultItem item, FilterGroup group)
    {
        foreach (var node in group.All)
        {
            if (node is Filter f && !FilterMatches(item, f))
            {
                return $"{f.Field} {f.Op} {f.Value}";
            }
        }
        foreach (var node in group.Not)
        {
            if (node is Filter f && FilterMatches(item, f))
            {
                return $"not({f.Field} {f.Op} {f.Value})";
            }
        }
        if (group.Any.Count > 0 && !group.Any.Any(n => EvaluateFilterNode(item, n)))
        {
            return "no-any-match";
        }

        return null;
    }

    internal static bool ShouldSkipAccessibility(MediathekResultItem item)
    {
        foreach (var keyword in AccessibilityKeywords)
        {
            if (item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool EvaluateFilterGroup(MediathekResultItem item, FilterGroup group)
    {
        if (group.IsEmpty)
        {
            return true;
        }

        foreach (var node in group.All)
        {
            if (!EvaluateFilterNode(item, node))
            {
                return false;
            }
        }

        if (group.Any.Count > 0)
        {
            var anyPass = false;
            foreach (var node in group.Any)
            {
                if (EvaluateFilterNode(item, node))
                {
                    anyPass = true;
                    break;
                }
            }
            if (!anyPass)
            {
                return false;
            }
        }

        foreach (var node in group.Not)
        {
            if (EvaluateFilterNode(item, node))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateFilterNode(MediathekResultItem item, FilterNode node) =>
        node switch
        {
            Filter filter => FilterMatches(item, filter),
            FilterGroup group => EvaluateFilterGroup(item, group),
            _ => false,
        };

    private static bool FilterMatches(MediathekResultItem item, Filter filter)
    {
        var fieldValue = GetFieldValue(item, filter.Field);

        return filter.Op switch
        {
            FilterOp.GreaterThan => TryParseDouble(fieldValue, filter.Field, out var actual) &&
                                    double.TryParse(filter.Value, CultureInfo.InvariantCulture, out var threshold) &&
                                    actual > threshold,
            FilterOp.LessThan => TryParseDouble(fieldValue, filter.Field, out var actual) &&
                                 double.TryParse(filter.Value, CultureInfo.InvariantCulture, out var threshold) &&
                                 actual < threshold,
            FilterOp.ExactMatch => fieldValue.Equals(filter.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Eq => fieldValue.Equals(filter.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Contains => fieldValue.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.NotContains => !fieldValue.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Regex => Regex.IsMatch(fieldValue, filter.Value),
            _ => false,
        };
    }

    private static bool TryParseDouble(string fieldValue, string field, out double result)
    {
        if (field.Equals("duration", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(fieldValue, CultureInfo.InvariantCulture, out var seconds))
        {
            result = seconds / 60.0;
            return true;
        }

        return double.TryParse(fieldValue, CultureInfo.InvariantCulture, out result);
    }

    private static string GetFieldValue(MediathekResultItem item, string field) =>
        field.ToLowerInvariant() switch
        {
            "duration" => item.Duration.ToString(CultureInfo.InvariantCulture),
            "title" => item.Title,
            "description" => item.Description,
            "topic" => item.Topic,
            "channel" => item.Channel,
            "timestamp" => item.Timestamp.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty,
        };

    internal static string? BuildTitle(MediathekResultItem item, IReadOnlyList<TitleRule> titleRules)
    {
        if (titleRules.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();

        foreach (var rule in titleRules)
        {
            switch (rule.Type)
            {
                case TitleRuleType.Static:
                    if (rule.Value is not null)
                    {
                        sb.Append(rule.Value);
                    }

                    break;

                case TitleRuleType.Regex:
                    if (rule.Pattern is null || rule.Field is null)
                    {
                        return null;
                    }

                    var fieldValue = GetFieldValue(item, rule.Field);
                    var match = Regex.Match(fieldValue, rule.Pattern);
                    if (!match.Success || match.Groups.Count < 2)
                    {
                        return null;
                    }

                    var captured = rule.CaptureGroup is not null
                        ? match.Groups[rule.CaptureGroup.Value]
                        : match.Groups[^1];
                    if (captured.Length == 0)
                    {
                        return null;
                    }

                    sb.Append(captured.Value);
                    break;
            }
        }

        var result = sb.ToString();
        return result.Length == 0 ? null : result;
    }

    private static MatchedEpisodeInfo? MatchSeasonAndEpisode(
        MediathekResultItem item, Rule rule,
        IReadOnlyList<TvdbEpisodeInfo> tvdbEpisodes, string showName)
    {
        if (rule.SeasonRegex is null || rule.EpisodeRegex is null)
        {
            return null;
        }

        var seasonMatch = Regex.Match(item.Title, rule.SeasonRegex);
        var episodeMatch = Regex.Match(item.Title, rule.EpisodeRegex);

        if (!seasonMatch.Success || !episodeMatch.Success)
        {
            return null;
        }

        var seasonGroup = rule.CaptureGroup is not null
            ? seasonMatch.Groups[rule.CaptureGroup.Value]
            : seasonMatch.Groups[^1];
        var episodeGroup = rule.CaptureGroup is not null
            ? episodeMatch.Groups[rule.CaptureGroup.Value]
            : episodeMatch.Groups[^1];

        if (!int.TryParse(seasonGroup.Value, CultureInfo.InvariantCulture, out var season) ||
            !int.TryParse(episodeGroup.Value, CultureInfo.InvariantCulture, out var episode))
        {
            return null;
        }

        var matchedEp = tvdbEpisodes.FirstOrDefault(e =>
            e.AiredSeason == season && e.AiredEpisodeNumber == episode);

        if (matchedEp is null)
        {
            return null;
        }

        return new MatchedEpisodeInfo
        {
            Episode = matchedEp,
            ShowName = showName,
            MatchedTitle = $"S{season:D2}E{episode:D2}",
        };
    }

    private static MatchedEpisodeInfo? MatchTitleExact(
        MediathekResultItem item, Rule rule,
        IReadOnlyList<TvdbEpisodeInfo> tvdbEpisodes, string showName)
    {
        var constructedTitle = BuildTitle(item, rule.TitleRules);
        if (constructedTitle is null)
        {
            return null;
        }

        var matches = tvdbEpisodes
            .Where(e => FormatTitle(e.EpisodeName)
                .Equals(FormatTitle(constructedTitle), StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var matchedEp = GuessCorrectMatch(item, matches);
        if (matchedEp is null)
        {
            return null;
        }

        return new MatchedEpisodeInfo
        {
            Episode = matchedEp,
            ShowName = showName,
            MatchedTitle = constructedTitle,
        };
    }

    private static MatchedEpisodeInfo? MatchTitleIncludes(
        MediathekResultItem item, Rule rule,
        IReadOnlyList<TvdbEpisodeInfo> tvdbEpisodes, string showName)
    {
        var constructedTitle = BuildTitle(item, rule.TitleRules);
        if (constructedTitle is null)
        {
            return null;
        }

        var matchedEp = tvdbEpisodes.FirstOrDefault(e =>
            FormatTitle(e.EpisodeName)
                .Contains(FormatTitle(constructedTitle), StringComparison.OrdinalIgnoreCase));

        if (matchedEp is null)
        {
            return null;
        }

        return new MatchedEpisodeInfo
        {
            Episode = matchedEp,
            ShowName = showName,
            MatchedTitle = constructedTitle,
        };
    }

    private static MatchedEpisodeInfo? MatchAirdate(
        MediathekResultItem item, Rule rule,
        IReadOnlyList<TvdbEpisodeInfo> tvdbEpisodes, string showName)
    {
        var constructedTitle = BuildTitle(item, rule.TitleRules);
        if (constructedTitle is null)
        {
            return null;
        }

        if (!TryParseGermanDate(constructedTitle, out var parsedDate))
        {
            return null;
        }

        var matchedEp = tvdbEpisodes.FirstOrDefault(e =>
            DateTime.TryParseExact(e.FirstAired, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var aired) &&
            aired.Date == parsedDate.Date);

        if (matchedEp is null)
        {
            return null;
        }

        return new MatchedEpisodeInfo
        {
            Episode = matchedEp,
            ShowName = showName,
            MatchedTitle = constructedTitle,
        };
    }

    private static MatchedEpisodeInfo? MatchAbsoluteEpisode(
        MediathekResultItem item, Rule rule,
        IReadOnlyList<TvdbEpisodeInfo> tvdbEpisodes, string showName)
    {
        if (rule.EpisodeRegex is null)
        {
            return null;
        }

        var match = Regex.Match(item.Title, rule.EpisodeRegex);
        if (!match.Success)
        {
            return null;
        }

        var captureGroup = rule.CaptureGroup is not null
            ? match.Groups[rule.CaptureGroup.Value]
            : match.Groups.Count > 1 ? match.Groups[^1] : match.Groups[0];
        if (!int.TryParse(captureGroup.Value, CultureInfo.InvariantCulture, out var absoluteNumber))
        {
            return null;
        }

        var matchedEp = tvdbEpisodes.FirstOrDefault(e =>
            e.AiredEpisodeNumber == absoluteNumber && e.AiredSeason <= 1);

        matchedEp ??= tvdbEpisodes.FirstOrDefault(e => e.AiredEpisodeNumber == absoluteNumber);

        if (matchedEp is null)
        {
            return null;
        }

        return new MatchedEpisodeInfo
        {
            Episode = matchedEp,
            ShowName = showName,
            MatchedTitle = $"Episode {absoluteNumber}",
        };
    }

    internal static bool TryParseGermanDate(string text, out DateTime result)
    {
        var germanCulture = new CultureInfo("de-DE");

        if (DateTime.TryParseExact(text.Trim(), "d. MMMM yyyy", germanCulture,
                DateTimeStyles.None, out result))
        {
            return true;
        }

        if (DateTime.TryParseExact(text.Trim(), "dd. MMMM yyyy", germanCulture,
                DateTimeStyles.None, out result))
        {
            return true;
        }

        if (DateTime.TryParseExact(text.Trim(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        if (DateTime.TryParseExact(text.Trim(), "d.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static TvdbEpisodeInfo? GuessCorrectMatch(MediathekResultItem item, TvdbEpisodeInfo[] matches)
    {
        if (matches.Length == 0)
        {
            return null;
        }

        if (matches.Length == 1)
        {
            return matches[0];
        }

        var itemDate = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp).UtcDateTime.Date;
        var byAirDate = matches.FirstOrDefault(e =>
            DateTime.TryParseExact(e.FirstAired, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var aired) &&
            aired.Date == itemDate);

        if (byAirDate is not null)
        {
            return byAirDate;
        }

        return matches
            .OrderByDescending(e => e.FirstAired)
            .First();
    }

    private static string FormatTitle(string title) =>
        title.Trim().Replace("  ", " ");
}
