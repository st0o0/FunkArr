using System.Globalization;
using System.Text.RegularExpressions;
using Akka.Actor;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.MatchMagic;

public sealed class MatchMagicActor : ReceiveActor
{
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly string[] _germanMonths =
    [
        "Januar", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember"
    ];

    public MatchMagicActor()
    {
        Receive<ExecuteScoring>(Handle);
    }

    private void Handle(ExecuteScoring msg)
    {
        var sortedRules = msg.Config.Rules.OrderBy(r => r.Priority).ToArray();
        var scored = new ScoredItem[msg.Items.Length];
        var itemTraces = new ItemTrace[msg.Items.Length];

        for (var i = 0; i < msg.Items.Length; i++)
        {
            var candidate = msg.Items[i];
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

                var confidence = rule.Confidence ?? msg.Config.DefaultConfidence;
                scored[i] = new ScoredItem(i, confidence, true);
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
                candidate.Title, candidate.Topic, candidate.Channel,
                candidate.Duration, candidate.Quality,
                candidate.Description, candidate.Timestamp,
                matched, scored[i].Score, matchedRuleId,
                identification, ruleTraces.ToArray());
        }

        Sender.Tell(new ScoreCompleted(msg.RequestId, scored));

        if (msg.HistoryRef != ActorRefs.Nobody)
        {
            var matchedCount = scored.Count(s => s.Matched);
            msg.HistoryRef.Tell(new RecordScoringResult(
                msg.RequestId, msg.Config.RuleSetId, msg.Origin,
                DateTimeOffset.UtcNow, msg.Items.Length, matchedCount, itemTraces));
        }
    }

    private static (bool passed, FilterGroupTrace? trace) EvaluateFiltersTraced(
        FilterSpec? filters, ScoreCandidate candidate)
    {
        if (filters is null)
        {
            return (true, null);
        }

        var subGroups = new List<FilterNodeTrace>();
        var overallPassed = true;

        if (filters.All is { Length: > 0 })
        {
            var (passed, trace) = EvaluateGroupTraced(filters.All, "All", candidate);
            subGroups.Add(new FilterNodeTrace(null, null, null, null, passed, false, trace));
            if (!passed)
            {
                overallPassed = false;
            }
        }

        if (filters.Any is { Length: > 0 })
        {
            var (passed, trace) = EvaluateGroupTraced(filters.Any, "Any", candidate);
            subGroups.Add(new FilterNodeTrace(null, null, null, null, passed, false, trace));
            if (!passed)
            {
                overallPassed = false;
            }
        }

        if (filters.Not is { Length: > 0 })
        {
            var (passed, trace) = EvaluateGroupTraced(filters.Not, "Not", candidate);
            subGroups.Add(new FilterNodeTrace(null, null, null, null, passed, false, trace));
            if (!passed)
            {
                overallPassed = false;
            }
        }

        if (subGroups.Count == 0)
        {
            return (true, null);
        }

        if (subGroups.Count == 1)
        {
            return (overallPassed, subGroups[0].Group);
        }

        return (overallPassed, new FilterGroupTrace("All", overallPassed, subGroups.ToArray()));
    }

    private static (bool passed, FilterGroupTrace trace) EvaluateGroupTraced(
        FilterNode[] nodes, string op, ScoreCandidate candidate)
    {
        var nodeTraces = new List<FilterNodeTrace>(nodes.Length);
        bool groupPassed;

        switch (op)
        {
            case "All":
                {
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
                }
            case "Any":
                {
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
                }
            case "Not":
                {
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
                }
            default:
                groupPassed = false;
                break;
        }

        return (groupPassed, new FilterGroupTrace(op, groupPassed, nodeTraces.ToArray()));
    }

    private static FilterNodeTrace CreateSkippedNodeTrace(FilterNode node) => node switch
    {
        FilterNode.ConditionNode c => new FilterNodeTrace(
            c.Condition.Field.ToString(), c.Condition.Op.ToString(),
            c.Condition.Value, null, false, true, null),
        FilterNode.GroupNode => new FilterNodeTrace(
            null, null, null, null, false, true, null),
        _ => new FilterNodeTrace(null, null, null, null, false, true, null),
    };

    private static (bool result, FilterNodeTrace trace) EvaluateNodeTraced(
        FilterNode node, ScoreCandidate candidate)
    {
        switch (node)
        {
            case FilterNode.ConditionNode c:
                {
                    var (result, actualValue) = EvaluateConditionTraced(c.Condition, candidate);
                    return (result, new FilterNodeTrace(
                        c.Condition.Field.ToString(), c.Condition.Op.ToString(),
                        c.Condition.Value, actualValue, result, false, null));
                }
            case FilterNode.GroupNode g:
                {
                    var (passed, groupTrace) = EvaluateFiltersTraced(g.Group, candidate);
                    return (passed, new FilterNodeTrace(
                        null, null, null, null, passed, false, groupTrace));
                }
            default:
                return (false, new FilterNodeTrace(null, null, null, null, false, false, null));
        }
    }

    private static (bool result, string? actualValue) EvaluateConditionTraced(
        FilterCondition condition, ScoreCandidate candidate)
    {
        var fieldValue = ResolveField(condition.Field, candidate);
        if (fieldValue is null)
        {
            return (false, null);
        }

        var result = condition.Op switch
        {
            FilterOp.Eq => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Contains => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.NotContains => !fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.GreaterThan => CompareNumeric(fieldValue, condition.Value) > 0,
            FilterOp.LessThan => CompareNumeric(fieldValue, condition.Value) < 0,
            FilterOp.Regex => EvaluateRegex(fieldValue, condition.Value),
            _ => false,
        };

        return (result, fieldValue);
    }

    private static string? ResolveField(FilterField field, ScoreCandidate candidate) => field switch
    {
        FilterField.Title => candidate.Title,
        FilterField.Topic => candidate.Topic,
        FilterField.Channel => candidate.Channel,
        FilterField.Duration => (candidate.Duration / 60).ToString(CultureInfo.InvariantCulture),
        FilterField.Description => candidate.Description,
        FilterField.Timestamp => candidate.Timestamp.ToString(CultureInfo.InvariantCulture),
        _ => null,
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
            return false;
        }
    }

    private static (bool success, IdentificationTrace trace, TracedIdentification? identification)
        IdentifyTraced(IdentificationSpec spec, ScoreCandidate candidate) => spec.Strategy switch
        {
            IdentificationStrategy.RegexCapture => IdentifyRegexCaptureTraced(spec, candidate),
            IdentificationStrategy.TitleConstruction => IdentifyTitleConstructionTraced(spec, candidate),
            IdentificationStrategy.AirdateExtraction => IdentifyAirdateTraced(candidate),
            _ => (false, new IdentificationTrace(null, false, "unknown strategy"), null),
        };

    private static (bool, IdentificationTrace, TracedIdentification?) IdentifyRegexCaptureTraced(
        IdentificationSpec spec, ScoreCandidate candidate)
    {
        string? season = null;

        if (spec.SeasonPattern is not null)
        {
            season = ExtractCapture(candidate.Title, spec.SeasonPattern, spec.CaptureGroup);
            if (season is null)
            {
                return (false,
                    new IdentificationTrace("RegexCapture", true, "season pattern did not match"),
                    null);
            }
        }

        if (spec.EpisodePattern is null)
        {
            return (false,
                new IdentificationTrace("RegexCapture", true, "no episode pattern configured"),
                null);
        }

        var episode = ExtractCapture(candidate.Title, spec.EpisodePattern, spec.CaptureGroup);
        if (episode is null)
        {
            return (false,
                new IdentificationTrace("RegexCapture", true, "episode pattern did not match"),
                null);
        }

        return (true,
            new IdentificationTrace("RegexCapture", true, null),
            new TracedIdentification(season, episode, null));
    }

    private static (bool, IdentificationTrace, TracedIdentification?) IdentifyTitleConstructionTraced(
        IdentificationSpec spec, ScoreCandidate candidate)
    {
        if (spec.TitleParts is not { Length: > 0 })
        {
            return (false,
                new IdentificationTrace("TitleConstruction", true, "no title parts configured"),
                null);
        }

        var constructedTitle = BuildTitle(spec.TitleParts, candidate);
        if (constructedTitle is null)
        {
            return (false,
                new IdentificationTrace("TitleConstruction", true, "title part regex did not match"),
                null);
        }

        var mode = spec.MatchMode ?? TitleMatchMode.Exact;

        var matched = mode switch
        {
            TitleMatchMode.Exact => string.Equals(constructedTitle, candidate.Title,
                StringComparison.OrdinalIgnoreCase),
            TitleMatchMode.Contains => NormalizeUmlauts(candidate.Title)
                .Contains(NormalizeUmlauts(constructedTitle), StringComparison.OrdinalIgnoreCase),
            _ => false,
        };

        if (!matched)
        {
            return (false,
                new IdentificationTrace("TitleConstruction", true, "title does not match constructed title"),
                null);
        }

        return (true,
            new IdentificationTrace("TitleConstruction", true, null),
            new TracedIdentification(null, null, constructedTitle));
    }

    private static (bool, IdentificationTrace, TracedIdentification?) IdentifyAirdateTraced(
        ScoreCandidate candidate)
    {
        var date = ExtractGermanDate(candidate.Title);
        if (date is null)
        {
            return (false,
                new IdentificationTrace("AirdateExtraction", true, "no date found in title"),
                null);
        }

        var formatted = date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return (true,
            new IdentificationTrace("AirdateExtraction", true, null),
            new TracedIdentification(null, null, formatted));
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
        _ => null,
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

            var groupIndex = captureGroup ?? (match.Groups.Count - 1);
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
        }
        catch (RegexMatchTimeoutException)
        {
            // Fall through
        }

        return null;
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
