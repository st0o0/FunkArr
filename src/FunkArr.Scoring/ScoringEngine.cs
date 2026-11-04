using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Terp.Diagnostics;

namespace FunkArr.Scoring;

public static class ScoringEngine
{
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly CultureInfo _germanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static ScoringResult Score(MatchingConfig config, ScoreCandidate[] items)
    {
        var sw = Stopwatch.StartNew();
        Telemetry.Runs.Up();
        var sortedRules = config.Rules.OrderBy(r => r.Priority).ToArray();
        var scored = new ScoredItem[items.Length];
        var itemTraces = new ItemTrace[items.Length];

        for (var i = 0; i < items.Length; i++)
        {
            var candidate = items[i];

            if (candidate.ExcludedVariant is { } excludedVariant)
            {
                scored[i] = new ScoredItem(i, 0.0, false);
                itemTraces[i] = new ItemTrace(candidate, false, 0.0, null, null, [], ExcludedVariant: excludedVariant);
                continue;
            }

            var matched = false;
            var ruleTraces = new List<RuleTrace>();
            string? matchedRuleId = null;
            TracedIdentification? identification = null;

            foreach (var rule in sortedRules)
            {
                var (filterPassed, filterTrace) = EvaluateFiltersTraced(rule.Filters, candidate);

                if (!filterPassed)
                {
                    ruleTraces.Add(new RuleTrace(
                        rule.Id, rule.Priority, RuleOutcome.FilterFailed,
                        filterTrace, new IdentificationTrace(null, false, null)));
                    continue;
                }

                var (idSuccess, idTrace, tracedId) = IdentifyTraced(rule.Identification, candidate);

                if (!idSuccess)
                {
                    ruleTraces.Add(new RuleTrace(
                        rule.Id, rule.Priority, RuleOutcome.IdentificationFailed,
                        filterTrace, idTrace));
                    continue;
                }

                var confidence = rule.Confidence ?? config.DefaultConfidence;
                var metadata = BuildMetadata(tracedId, candidate);
                scored[i] = new ScoredItem(i, confidence, true, metadata);
                matched = true;
                matchedRuleId = rule.Id;
                identification = tracedId;
                ruleTraces.Add(new RuleTrace(
                    rule.Id, rule.Priority, RuleOutcome.Matched,
                    filterTrace, idTrace));
                break;
            }

            if (!matched)
            {
                scored[i] = new ScoredItem(i, 0.0, false);
            }

            itemTraces[i] = new ItemTrace(
                candidate, matched, scored[i].Score, matchedRuleId,
                identification, [.. ruleTraces]);
        }

        var accepted = scored.Count(s => s.Matched);
        var rejected = scored.Length - accepted;
        var ruleSetTag = new KeyValuePair<string, object?>("ruleSetId", config.RuleSetId);
        Telemetry.Accepted.Add(accepted, ruleSetTag);
        Telemetry.Rejected.Add(rejected, ruleSetTag);
        sw.Stop();
        Telemetry.Duration.Record(sw.Elapsed.TotalSeconds);
        return new ScoringResult(scored, itemTraces);
    }

    private static FilterEvaluation EvaluateFiltersTraced(FilterSpec? filters, ScoreCandidate candidate)
    {
        if (filters is null)
        {
            return new FilterEvaluation(true, null);
        }

        var subGroups = new List<FilterNodeTrace>();
        var overallPassed = true;

        if (filters.All is { Length: > 0 })
        {
            var (passed, trace) = EvaluateGroupTraced(filters.All, FilterGroupOp.All, candidate);
            subGroups.Add(new FilterNodeTrace(null, null, null, null, passed, false, trace));
            if (!passed)
            {
                overallPassed = false;
            }
        }

        if (filters.Any is { Length: > 0 })
        {
            var (passed, trace) = EvaluateGroupTraced(filters.Any, FilterGroupOp.Any, candidate);
            subGroups.Add(new FilterNodeTrace(null, null, null, null, passed, false, trace));
            if (!passed)
            {
                overallPassed = false;
            }
        }

        if (filters.Not is { Length: > 0 })
        {
            var (passed, trace) = EvaluateGroupTraced(filters.Not, FilterGroupOp.Not, candidate);
            subGroups.Add(new FilterNodeTrace(null, null, null, null, passed, false, trace));
            if (!passed)
            {
                overallPassed = false;
            }
        }

        return subGroups.Count switch
        {
            0 => new FilterEvaluation(true, null),
            1 => new FilterEvaluation(overallPassed, subGroups[0].Group),
            _ => new FilterEvaluation(overallPassed, new FilterGroupTrace(FilterGroupOp.All, overallPassed, [.. subGroups]))
        };
    }

    private static FilterEvaluation EvaluateGroupTraced(FilterNode[] nodes, FilterGroupOp op, ScoreCandidate candidate)
    {
        var nodeTraces = new List<FilterNodeTrace>(nodes.Length);
        bool groupPassed;

        switch (op)
        {
            case FilterGroupOp.All:
                var failed = false;
                foreach (var node in nodes)
                {
                    if (failed)
                    {
                        nodeTraces.Add(CreateSkippedNodeTrace(node));
                        continue;
                    }

                    var (result, trace) = EvaluateNodeTraced(node, candidate);
                    nodeTraces.Add(trace);
                    if (!result)
                    {
                        failed = true;
                    }
                }

                groupPassed = !failed;
                break;
            case FilterGroupOp.Any:
                var found = false;
                foreach (var node in nodes)
                {
                    if (found)
                    {
                        nodeTraces.Add(CreateSkippedNodeTrace(node));
                        continue;
                    }

                    var (result, trace) = EvaluateNodeTraced(node, candidate);
                    nodeTraces.Add(trace);
                    if (result)
                    {
                        found = true;
                    }
                }

                groupPassed = found;
                break;
            case FilterGroupOp.Not:
                var anyMatched = false;
                foreach (var node in nodes)
                {
                    if (anyMatched)
                    {
                        nodeTraces.Add(CreateSkippedNodeTrace(node));
                        continue;
                    }

                    var (result, trace) = EvaluateNodeTraced(node, candidate);
                    nodeTraces.Add(trace);
                    if (result)
                    {
                        anyMatched = true;
                    }
                }

                groupPassed = !anyMatched;
                break;
            default:
                groupPassed = false;
                break;
        }

        return new FilterEvaluation(groupPassed, new FilterGroupTrace(op, groupPassed, [.. nodeTraces]));
    }

    private static FilterNodeTrace CreateSkippedNodeTrace(FilterNode node) => node switch
    {
        FilterNode.ConditionNode c => new FilterNodeTrace(
            c.Condition.Field.ToString(), c.Condition.Op.ToString(),
            c.Condition.Value, null, false, true, null),
        _ => new FilterNodeTrace(null, null, null, null, false, true, null)
    };

    private static NodeEvaluation EvaluateNodeTraced(
        FilterNode node, ScoreCandidate candidate)
    {
        switch (node)
        {
            case FilterNode.ConditionNode c:
                {
                    var (result, actualValue) = EvaluateConditionTraced(c.Condition, candidate);
                    return new NodeEvaluation(result, new FilterNodeTrace(
                        c.Condition.Field.ToString(), c.Condition.Op.ToString(),
                        c.Condition.Value, actualValue, result, false, null));
                }
            case FilterNode.GroupNode g:
                {
                    var (passed, groupTrace) = EvaluateFiltersTraced(g.Group, candidate);
                    return new NodeEvaluation(passed, new FilterNodeTrace(
                        null, null, null, null, passed, false, groupTrace));
                }
            default:
                return new NodeEvaluation(false, new FilterNodeTrace(null, null, null, null, false, false, null));
        }
    }

    private static ConditionResult EvaluateConditionTraced(
        FilterCondition condition, ScoreCandidate candidate)
    {
        var fieldValue = ResolveField(condition.Field, candidate);
        if (fieldValue is null)
        {
            return new ConditionResult(false, null);
        }

        var result = condition.Op switch
        {
            FilterOp.Eq => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Contains => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.NotContains => !fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.GreaterThan => CompareNumeric(fieldValue, condition.Value) > 0,
            FilterOp.LessThan => CompareNumeric(fieldValue, condition.Value) < 0,
            FilterOp.Regex => EvaluateRegex(fieldValue, condition.Value),
            _ => false
        };

        return new ConditionResult(result, fieldValue);
    }

    private static string? ResolveField(FilterField field, ScoreCandidate candidate) => field switch
    {
        FilterField.Title => candidate.Title,
        FilterField.Topic => candidate.Topic,
        FilterField.Channel => candidate.Channel,
        FilterField.Duration => (candidate.Duration / 60).ToString(CultureInfo.InvariantCulture),
        FilterField.Description => candidate.Description,
        FilterField.Timestamp => candidate.Timestamp.ToString(CultureInfo.InvariantCulture),
        _ => null
    };

    private static int CompareNumeric(string left, string right)
    {
        if (double.TryParse(left, CultureInfo.InvariantCulture, out var l) &&
            double.TryParse(right, CultureInfo.InvariantCulture, out var r))
        {
            return l.CompareTo(r);
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateRegex(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.None, _regexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            Telemetry.RegexTimeouts.Up();
            return false;
        }
    }

    private static IdentificationResult IdentifyTraced(IdentificationSpec spec, ScoreCandidate candidate) =>
        spec.Strategy switch
        {
            IdentificationStrategy.SeasonAndEpisodeNumber => IdentifyRegexCaptureTraced(spec, candidate),
            IdentificationStrategy.AbsoluteEpisodeNumber => IdentifyRegexCaptureTraced(spec, candidate),
            IdentificationStrategy.TitleExact => IdentifyTitleConstructionTraced(spec, candidate, exact: true),
            IdentificationStrategy.TitleIncludes => IdentifyTitleConstructionTraced(spec, candidate, exact: false),
            IdentificationStrategy.AirdateExtraction => IdentifyAirdateTraced(candidate),
            _ => new IdentificationResult(
                false, new IdentificationTrace(null, false, IdentificationFailureReason.UnknownStrategy), null)
        };

    private static IdentificationResult IdentifyRegexCaptureTraced(
        IdentificationSpec spec, ScoreCandidate candidate)
    {
        string? season = null;

        if (spec.SeasonPattern is not null)
        {
            season = ExtractCapture(candidate.Title, spec.SeasonPattern, spec.CaptureGroup);
            if (season is null)
            {
                return IdentificationResult.Failed(spec.Strategy, IdentificationFailureReason.SeasonPatternNotMatched);
            }
        }

        if (spec.EpisodePattern is null)
        {
            return IdentificationResult.Failed(spec.Strategy, IdentificationFailureReason.NoEpisodePatternConfigured);
        }

        var episode = ExtractCapture(candidate.Title, spec.EpisodePattern, spec.CaptureGroup);
        if (episode is null)
        {
            return IdentificationResult.Failed(spec.Strategy, IdentificationFailureReason.EpisodePatternNotMatched);
        }

        return IdentificationResult.Matched(spec.Strategy, new TracedIdentification(season, episode, null));
    }

    private static IdentificationResult IdentifyTitleConstructionTraced(
        IdentificationSpec spec, ScoreCandidate candidate, bool exact = true)
    {
        if (spec.TitleParts is not { Length: > 0 })
        {
            return IdentificationResult.Failed(spec.Strategy, IdentificationFailureReason.NoTitlePartsConfigured);
        }

        var constructedTitle = BuildTitle(spec.TitleParts, candidate);
        if (constructedTitle is null)
        {
            return IdentificationResult.Failed(spec.Strategy, IdentificationFailureReason.TitlePartRegexNotMatched);
        }

        var matched = exact
            ? string.Equals(constructedTitle, candidate.Title, StringComparison.OrdinalIgnoreCase)
            : NormalizeUmlauts(candidate.Title)
                .Contains(NormalizeUmlauts(constructedTitle), StringComparison.OrdinalIgnoreCase);

        if (!matched)
        {
            return IdentificationResult.Failed(spec.Strategy, IdentificationFailureReason.TitleDoesNotMatch);
        }

        return IdentificationResult.Matched(spec.Strategy, new TracedIdentification(null, null, constructedTitle));
    }

    private static IdentificationResult IdentifyAirdateTraced(ScoreCandidate candidate)
    {
        var date = ExtractGermanDate(candidate.Title);
        if (date is null)
        {
            return IdentificationResult.Failed(
                IdentificationStrategy.AirdateExtraction, IdentificationFailureReason.NoDateFoundInTitle);
        }

        var formatted = date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return IdentificationResult.Matched(
            IdentificationStrategy.AirdateExtraction, new TracedIdentification(null, null, formatted));
    }

    private static string? BuildTitle(TitlePart[] parts, ScoreCandidate candidate)
    {
        var segments = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            switch (part.Type)
            {
                case TitlePartType.Static:
                    segments.Add(part.Value ?? "");
                    break;

                case TitlePartType.Regex when part.Pattern is not null:
                    var fieldValue = ResolveFieldForTitlePart(part.Field, candidate);
                    if (fieldValue is null)
                    {
                        return null;
                    }

                    var captured = ExtractCapture(fieldValue, part.Pattern, part.CaptureGroup);
                    if (captured is null)
                    {
                        return null;
                    }

                    segments.Add(captured);
                    break;

                default:
                    return null;
            }
        }

        return string.Concat(segments);
    }

    private static string? ResolveFieldForTitlePart(FilterField? field, ScoreCandidate candidate) => field switch
    {
        FilterField.Title => candidate.Title,
        FilterField.Topic => candidate.Topic,
        FilterField.Channel => candidate.Channel,
        FilterField.Description => candidate.Description,
        null => candidate.Title,
        _ => null
    };

    private static string? ExtractCapture(string input, string pattern, int? captureGroup = null)
    {
        try
        {
            var match = Regex.Match(input, pattern, RegexOptions.None, _regexTimeout);
            if (!match.Success)
            {
                return null;
            }

            var groupIndex = captureGroup ?? match.Groups.Count - 1;
            if (groupIndex < 0 || groupIndex >= match.Groups.Count)
            {
                return null;
            }

            var captured = match.Groups[groupIndex].Value;
            return string.IsNullOrEmpty(captured) ? null : captured;
        }
        catch (RegexMatchTimeoutException)
        {
            Telemetry.RegexTimeouts.Up();
            return null;
        }
    }

    private static DateTime? ExtractGermanDate(string text)
    {
        try
        {
            var numericMatch = Regex.Match(text, @"(\d{1,2})\.(\d{1,2})\.(\d{4}|\d{2})", RegexOptions.None,
                _regexTimeout);
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

            var longMatch = Regex.Match(text, @"(\d{1,2})\.\s*(\w+)\s+(\d{4})", RegexOptions.None, _regexTimeout);
            if (longMatch.Success &&
                DateTime.TryParseExact(longMatch.Value, "d. MMMM yyyy", _germanCulture,
                    DateTimeStyles.None, out var germanDate))
            {
                return germanDate;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            Telemetry.RegexTimeouts.Up();
        }

        return null;
    }

    private static MetadataSpec? BuildMetadata(TracedIdentification? id, ScoreCandidate candidate)
    {
        if (id is null)
        {
            return null;
        }

        DateTimeOffset? airedAt = candidate.Timestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(candidate.Timestamp)
            : null;

        return new MetadataSpec(id.Season, id.Episode, airedAt, id.Title);
    }

    private static string NormalizeUmlauts(string input) => input
        .Replace("ä", "ae", StringComparison.Ordinal)
        .Replace("ö", "oe", StringComparison.Ordinal)
        .Replace("ü", "ue", StringComparison.Ordinal)
        .Replace("Ä", "Ae", StringComparison.Ordinal)
        .Replace("Ö", "Oe", StringComparison.Ordinal)
        .Replace("Ü", "Ue", StringComparison.Ordinal)
        .Replace("ß", "ss", StringComparison.Ordinal);
}
