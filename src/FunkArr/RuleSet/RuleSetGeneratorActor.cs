using System.Globalization;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Event;
using FunkArr.Search;
using FunkArr.Shared;

namespace FunkArr.RuleSet;

public sealed partial class RuleSetGeneratorActor : ReceiveActor
{
    private readonly MediathekClient _mediathekClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public sealed record GenerateRuleSet(int TvdbId, string ShowName);

    public RuleSetGeneratorActor(MediathekClient mediathekClient)
    {
        _mediathekClient = mediathekClient;

        ReceiveAsync<GenerateRuleSet>(HandleGenerateAsync);
    }

    private async Task HandleGenerateAsync(GenerateRuleSet request)
    {
        try
        {
            _log.Info("Starting ruleset generation for '{ShowName}' (tvdbId={TvdbId})",
                request.ShowName, request.TvdbId);

            var results = await SampleMediathekAsync(request.ShowName);
            if (results.Length == 0)
            {
                _log.Warning("No Mediathek results for '{ShowName}'", request.ShowName);
                Context.Parent.Tell(new RuleSetRegistryActor.GenerationFailed(request.TvdbId));
                Context.Stop(Self);
                return;
            }

            var topic = FindBestTopic(results, request.ShowName);
            if (topic is null)
            {
                _log.Warning("No matching topic for '{ShowName}'", request.ShowName);
                Context.Parent.Tell(new RuleSetRegistryActor.GenerationFailed(request.TvdbId));
                Context.Stop(Self);
                return;
            }

            var filtered = results
                .Where(r => r.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase))
                .Where(r => !ContentFilter.IsAccessibilityVariant(r.Title))
                .Take(15)
                .ToArray();

            if (filtered.Length == 0)
            {
                Context.Parent.Tell(new RuleSetRegistryActor.GenerationFailed(request.TvdbId));
                Context.Stop(Self);
                return;
            }

            var analysis = AnalyzePatterns(filtered, topic);
            var strategy = DetectStrategy(analysis);
            var (seasonRegex, episodeRegex, titleRules) = GenerateRegex(filtered, strategy, topic);
            var durationFilter = DeriveDurationFilter(filtered);

            var accessibilityFilter = new Filter
            {
                Field = "title",
                Op = FilterOp.Regex,
                Value = "(?i)audiodesk|gebärden|gebardensprache|hörfassung|klare sprache",
            };

            var filterGroup = durationFilter is not null
                ? new FilterGroup { All = [durationFilter], Not = [accessibilityFilter] }
                : new FilterGroup { Not = [accessibilityFilter] };

            var rule = new Rule
            {
                Priority = 0,
                Filters = filterGroup,
                Strategy = strategy,
                SeasonRegex = seasonRegex,
                EpisodeRegex = episodeRegex,
                TitleRules = titleRules,
            };

            var confidence = ComputeConfidence(filtered, rule);

            rule = rule with { Confidence = confidence };

            if (confidence < 0.3)
            {
                rule = rule with
                {
                    Strategy = MatchingStrategy.ItemTitleIncludes,
                    SeasonRegex = null,
                    EpisodeRegex = null,
                    TitleRules = GenerateFallbackTitleRules(topic),
                    Confidence = 0.3,
                };
                confidence = 0.3;
            }

            var ruleSet = new RuleSetFile
            {
                Topic = topic,
                Media = new MediaReference
                {
                    TvdbId = request.TvdbId,
                    Name = request.ShowName,
                },
                Source = "generated",
                Confidence = confidence,
                Rules = [rule],
            };

            _log.Info(
                "Generated ruleset for '{Topic}' with strategy {Strategy}, confidence {Confidence:F2}",
                topic, strategy, confidence);

            Context.Parent.Tell(new RuleSetRegistryActor.GenerationComplete(ruleSet));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Ruleset generation failed for '{ShowName}'", request.ShowName);
            Context.Parent.Tell(new RuleSetRegistryActor.GenerationFailed(request.TvdbId));
        }
        finally
        {
            Context.Stop(Self);
        }
    }

    private async Task<MediathekResultItem[]> SampleMediathekAsync(string showName)
    {
        var query = new MediathekQuery
        {
            Queries = [new MediathekQueryItem { Fields = ["topic", "title"], Query = showName }],
            Size = 50,
        };

        var response = await _mediathekClient.QueryAsync(query);
        return response?.Result ?? [];
    }

    internal static string? FindBestTopic(MediathekResultItem[] results, string showName)
    {
        if (results.Length == 0)
        {
            return null;
        }

        var topics = results.Select(r => r.Topic).Distinct().ToArray();
        var nameLower = showName.ToLowerInvariant();

        foreach (var topic in topics)
        {
            if (topic.Equals(showName, StringComparison.OrdinalIgnoreCase))
            {
                return topic;
            }
        }

        foreach (var topic in topics)
        {
            var topicLower = topic.ToLowerInvariant();
            if (topicLower.Contains(nameLower) || nameLower.Contains(topicLower))
            {
                return topic;
            }
        }

        if (topics.Length == 1)
        {
            return topics[0];
        }

        return null;
    }

    internal static PatternAnalysis AnalyzePatterns(MediathekResultItem[] samples, string topic)
    {
        var analysis = new PatternAnalysis();
        var topicLower = topic.ToLowerInvariant();

        analysis.Total = samples.Length;

        foreach (var item in samples)
        {
            var title = item.Title;

            if (ParenSeasonEpisode().IsMatch(title) ||
                BareSeasonEpisode().IsMatch(title) ||
                StaffelFolge().IsMatch(title))
            {
                analysis.SeasonEpisodeCount++;
            }

            if (DateVom().IsMatch(title) ||
                DateNumeric().IsMatch(title))
            {
                analysis.DateCount++;
            }

            if (AbsoluteEpisode().IsMatch(title) ||
                AbsoluteFolge().IsMatch(title) ||
                AbsoluteTeil().IsMatch(title) ||
                ParenAbsoluteNumber().IsMatch(title))
            {
                analysis.AbsoluteEpisodeCount++;
            }

            if (title.ToLowerInvariant().StartsWith(topicLower))
            {
                analysis.TopicPrefixCount++;
            }

            if (title.Contains(':') || title.Contains(" - "))
            {
                analysis.SeparatorCount++;
            }
        }

        return analysis;
    }

    internal static MatchingStrategy DetectStrategy(PatternAnalysis analysis)
    {
        if (analysis.SeasonEpisodeCount >= 3 &&
            analysis.SeasonEpisodeCount > analysis.DateCount)
        {
            return MatchingStrategy.SeasonAndEpisodeNumber;
        }

        if (analysis.DateCount >= 3 &&
            analysis.DateCount > analysis.SeasonEpisodeCount)
        {
            return MatchingStrategy.ItemTitleEqualsAirdate;
        }

        if (analysis.AbsoluteEpisodeCount >= 3)
        {
            return MatchingStrategy.ByAbsoluteEpisodeNumber;
        }

        if (analysis.TopicPrefixCount >= 3 &&
            analysis.SeparatorCount >= analysis.Total * 0.3)
        {
            return MatchingStrategy.ItemTitleExact;
        }

        return MatchingStrategy.ItemTitleIncludes;
    }

    internal static (string? seasonRegex, string? episodeRegex, IReadOnlyList<TitleRule> titleRules)
        GenerateRegex(MediathekResultItem[] samples, MatchingStrategy strategy, string topic)
    {
        var escapedTopic = Regex.Escape(topic);

        if (strategy == MatchingStrategy.SeasonAndEpisodeNumber)
        {
            foreach (var sample in samples.Take(5))
            {
                if (ParenSeasonEpisode().IsMatch(sample.Title))
                {
                    return (@"(?<=S)(\d{1,4})(?=/E)", @"(?<=E)(\d{1,4})(?=\))", []);
                }

                if (BareSeasonEpisode().IsMatch(sample.Title))
                {
                    return (@"(?<=S)(\d{1,4})(?=E)", @"(?<=E)(\d{1,4})", []);
                }

                if (StaffelFolge().IsMatch(sample.Title))
                {
                    return (@"Staffel\s*(\d+)", @"Folge\s*(\d+)", []);
                }
            }
        }

        if (strategy == MatchingStrategy.ItemTitleEqualsAirdate)
        {
            foreach (var sample in samples.Take(5))
            {
                if (DateVom().IsMatch(sample.Title))
                {
                    return (null, null, [new TitleRule
                    {
                        Type = TitleRuleType.Regex,
                        Field = "title",
                        Pattern = @"vom\s+(\d{1,2}\.\s*\w+\s*\d{4})",
                    }]);
                }

                if (DateNumeric().IsMatch(sample.Title))
                {
                    return (null, null, [new TitleRule
                    {
                        Type = TitleRuleType.Regex,
                        Field = "title",
                        Pattern = @"(\d{1,2}\.\d{1,2}\.\d{4})",
                    }]);
                }
            }
        }

        if (strategy == MatchingStrategy.ByAbsoluteEpisodeNumber)
        {
            foreach (var sample in samples.Take(5))
            {
                if (AbsoluteEpisode().IsMatch(sample.Title))
                {
                    return (null, @"Episode\s*(\d+)", []);
                }

                if (AbsoluteFolge().IsMatch(sample.Title))
                {
                    return (null, @"Folge\s*(\d+)", []);
                }

                if (AbsoluteTeil().IsMatch(sample.Title))
                {
                    return (null, @"Teil\s*(\d+)", []);
                }

                if (ParenAbsoluteNumber().IsMatch(sample.Title))
                {
                    return (null, @"\((\d{3,})\)", []);
                }
            }
        }

        if (strategy == MatchingStrategy.ItemTitleExact)
        {
            foreach (var sample in samples.Take(5))
            {
                if (sample.Title.Contains(':'))
                {
                    return (null, null, [new TitleRule
                    {
                        Type = TitleRuleType.Regex,
                        Field = "title",
                        Pattern = $"^{escapedTopic}[^:]*:\\s*(.+)",
                    }]);
                }

                if (sample.Title.Contains(" - "))
                {
                    return (null, null, [new TitleRule
                    {
                        Type = TitleRuleType.Regex,
                        Field = "title",
                        Pattern = $"^{escapedTopic}[^-]*-\\s*(.+)",
                    }]);
                }
            }
        }

        return (null, null, GenerateFallbackTitleRules(topic));
    }

    private static IReadOnlyList<TitleRule> GenerateFallbackTitleRules(string topic)
    {
        var escapedTopic = Regex.Escape(topic);
        return
        [
            new TitleRule
            {
                Type = TitleRuleType.Regex,
                Field = "title",
                Pattern = $"^{escapedTopic}\\s*[:\\-]?\\s*(.+)",
            },
        ];
    }

    internal static Filter? DeriveDurationFilter(MediathekResultItem[] samples)
    {
        if (samples.Length == 0)
        {
            return null;
        }

        var durations = samples
            .Select(s => s.Duration / 60.0)
            .OrderBy(d => d)
            .ToArray();

        var median = durations[durations.Length / 2];
        var threshold = (int)Math.Floor(median * 0.5);

        if (threshold <= 0)
        {
            return null;
        }

        return new Filter
        {
            Field = "duration",
            Op = FilterOp.GreaterThan,
            Value = threshold.ToString(CultureInfo.InvariantCulture),
        };
    }

    internal static double ComputeConfidence(MediathekResultItem[] samples, Rule rule)
    {
        var matchCount = 0;

        foreach (var item in samples)
        {
            if (!RuleSetMatchingEngine.EvaluateFilterGroup(item, rule.Filters))
            {
                continue;
            }

            var hasStructuredMatch = rule.Strategy switch
            {
                MatchingStrategy.SeasonAndEpisodeNumber =>
                    rule.SeasonRegex is not null && rule.EpisodeRegex is not null &&
                    Regex.IsMatch(item.Title, rule.SeasonRegex) &&
                    Regex.IsMatch(item.Title, rule.EpisodeRegex),
                MatchingStrategy.ItemTitleEqualsAirdate =>
                    RuleSetMatchingEngine.BuildTitle(item, rule.TitleRules) is not null,
                MatchingStrategy.ByAbsoluteEpisodeNumber =>
                    rule.EpisodeRegex is not null && Regex.IsMatch(item.Title, rule.EpisodeRegex),
                MatchingStrategy.ItemTitleExact or MatchingStrategy.ItemTitleIncludes =>
                    RuleSetMatchingEngine.BuildTitle(item, rule.TitleRules) is not null,
                _ => false,
            };

            if (hasStructuredMatch)
            {
                matchCount++;
            }
        }

        var matchRate = (double)matchCount / samples.Length;

        return matchRate switch
        {
            > 0.6 => Math.Min(0.95, 0.6 + matchRate * 0.35),
            > 0.3 => 0.5,
            _ => 0.3,
        };
    }

    [GeneratedRegex(@"\(S\d{1,4}/E\d{1,4}\)")]
    private static partial Regex ParenSeasonEpisode();

    [GeneratedRegex(@"\bS\d{1,4}E\d{1,4}\b")]
    private static partial Regex BareSeasonEpisode();

    [GeneratedRegex(@"Staffel\s*\d+.*Folge\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex StaffelFolge();

    [GeneratedRegex(@"vom\s+\d{1,2}\.\s*\w+\s*\d{4}")]
    private static partial Regex DateVom();

    [GeneratedRegex(@"\b\d{1,2}\.\d{1,2}\.\d{4}\b")]
    private static partial Regex DateNumeric();

    [GeneratedRegex(@"Episode\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex AbsoluteEpisode();

    [GeneratedRegex(@"Folge\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex AbsoluteFolge();

    [GeneratedRegex(@"Teil\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex AbsoluteTeil();

    [GeneratedRegex(@"\(\d{3,}\)")]
    private static partial Regex ParenAbsoluteNumber();

    internal record PatternAnalysis
    {
        public int SeasonEpisodeCount { get; set; }
        public int DateCount { get; set; }
        public int AbsoluteEpisodeCount { get; set; }
        public int TopicPrefixCount { get; set; }
        public int SeparatorCount { get; set; }
        public int Total { get; set; }
    }
}
