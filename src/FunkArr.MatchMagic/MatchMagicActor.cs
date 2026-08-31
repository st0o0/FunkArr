using System.Globalization;
using System.Text.RegularExpressions;
using Akka.Actor;
using FunkArr.Messages.Scoring;

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

        for (var i = 0; i < msg.Items.Length; i++)
        {
            var candidate = msg.Items[i];
            var matched = false;

            foreach (var rule in sortedRules)
            {
                if (!EvaluateFilters(rule.Filters, candidate))
                {
                    continue;
                }

                if (!Identify(rule.Identification, candidate))
                {
                    continue;
                }

                var confidence = rule.Confidence ?? msg.Config.DefaultConfidence;
                scored[i] = new ScoredItem(i, confidence, true);
                matched = true;
                break;
            }

            if (!matched)
            {
                scored[i] = new ScoredItem(i, 0.0, false);
            }
        }

        Sender.Tell(new ScoreCompleted(scored));
    }

    private static bool EvaluateFilters(FilterSpec? filters, ScoreCandidate candidate)
    {
        if (filters is null)
        {
            return true;
        }

        if (filters.All is { Length: > 0 } && !filters.All.All(n => EvaluateNode(n, candidate)))
        {
            return false;
        }

        if (filters.Any is { Length: > 0 } && !filters.Any.Any(n => EvaluateNode(n, candidate)))
        {
            return false;
        }

        if (filters.Not is { Length: > 0 } && filters.Not.Any(n => EvaluateNode(n, candidate)))
        {
            return false;
        }

        return true;
    }

    private static bool EvaluateNode(FilterNode node, ScoreCandidate candidate) => node switch
    {
        FilterNode.ConditionNode c => EvaluateCondition(c.Condition, candidate),
        FilterNode.GroupNode g => EvaluateFilters(g.Group, candidate),
        _ => false,
    };

    private static bool EvaluateCondition(FilterCondition condition, ScoreCandidate candidate)
    {
        var fieldValue = ResolveField(condition.Field, candidate);
        if (fieldValue is null)
        {
            return false;
        }

        return condition.Op switch
        {
            FilterOp.Eq => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Contains => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.NotContains => !fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.GreaterThan => CompareNumeric(fieldValue, condition.Value) > 0,
            FilterOp.LessThan => CompareNumeric(fieldValue, condition.Value) < 0,
            FilterOp.Regex => EvaluateRegex(fieldValue, condition.Value),
            _ => false,
        };
    }

    private static string? ResolveField(FilterField field, ScoreCandidate candidate) => field switch
    {
        FilterField.Title => candidate.Title,
        FilterField.Topic => candidate.Topic,
        FilterField.Channel => candidate.Channel,
        FilterField.Duration => (candidate.Duration / 60).ToString(CultureInfo.InvariantCulture),
        FilterField.Description => null,
        FilterField.Timestamp => "0",
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

    private static bool Identify(IdentificationSpec spec, ScoreCandidate candidate) => spec.Strategy switch
    {
        IdentificationStrategy.RegexCapture => IdentifyRegexCapture(spec, candidate),
        IdentificationStrategy.TitleConstruction => IdentifyTitleConstruction(spec, candidate),
        IdentificationStrategy.AirdateExtraction => IdentifyAirdate(candidate),
        _ => false,
    };

    private static bool IdentifyRegexCapture(IdentificationSpec spec, ScoreCandidate candidate)
    {
        if (spec.SeasonPattern is not null)
        {
            var season = ExtractCapture(candidate.Title, spec.SeasonPattern, spec.CaptureGroup);
            if (season is null)
            {
                return false;
            }
        }

        if (spec.EpisodePattern is null)
        {
            return false;
        }

        var episode = ExtractCapture(candidate.Title, spec.EpisodePattern, spec.CaptureGroup);
        return episode is not null;
    }

    private static bool IdentifyTitleConstruction(IdentificationSpec spec, ScoreCandidate candidate)
    {
        if (spec.TitleParts is not { Length: > 0 })
        {
            return false;
        }

        var constructedTitle = BuildTitle(spec.TitleParts, candidate);
        if (constructedTitle is null)
        {
            return false;
        }

        var mode = spec.MatchMode ?? TitleMatchMode.Exact;

        return mode switch
        {
            TitleMatchMode.Exact => string.Equals(constructedTitle, candidate.Title,
                StringComparison.OrdinalIgnoreCase),
            TitleMatchMode.Contains => NormalizeUmlauts(candidate.Title)
                .Contains(NormalizeUmlauts(constructedTitle), StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
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
        FilterField.Description => null,
        null => candidate.Title,
        _ => null,
    };

    private static bool IdentifyAirdate(ScoreCandidate candidate) => ExtractGermanDate(candidate.Title) is not null;

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
