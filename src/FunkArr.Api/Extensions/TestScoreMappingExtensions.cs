using FunkArr.Api.Models;
using FunkArr.Messages.Scoring;

namespace FunkArr.Api.Extensions;

internal static class TestScoreMappingExtensions
{
    internal static (MatchingConfig Config, ScoreCandidate[] Candidates) ToMessage(this TestScoreRequest request)
    {
        var rules = (request.Rules ?? [])
            .Select(r => r.ToMessage())
            .Where(r => r is not null)
            .Select(r => r!)
            .ToArray();

        var config = new MatchingConfig("test", request.DefaultConfidence, rules);
        var candidates = (request.Candidates ?? []).Select(c => c.ToMessage()).ToArray();

        return (config, candidates);
    }

    internal static MatchingRule? ToMessage(this RuleInput rule)
    {
        var identification = rule.ToIdentification();
        if (identification is null)
        {
            return null;
        }

        var filters = rule.Filters is not null ? rule.Filters.ToMessage() : null;

        return new MatchingRule(rule.Id, rule.Priority, rule.Confidence, filters, identification);
    }

    private static IdentificationSpec? ToIdentification(this RuleInput rule) => rule.Strategy switch
    {
        IdentificationStrategy.SeasonAndEpisodeNumber => new IdentificationSpec(
            IdentificationStrategy.SeasonAndEpisodeNumber,
            SeasonPattern: rule.SeasonRegex,
            EpisodePattern: rule.EpisodeRegex,
            CaptureGroup: rule.CaptureGroup),

        IdentificationStrategy.AbsoluteEpisodeNumber => new IdentificationSpec(
            IdentificationStrategy.AbsoluteEpisodeNumber,
            EpisodePattern: rule.EpisodeRegex,
            CaptureGroup: rule.CaptureGroup),

        IdentificationStrategy.TitleExact => new IdentificationSpec(
            IdentificationStrategy.TitleExact,
            TitleParts: rule.TitleRules?.ToMessageParts()),

        IdentificationStrategy.TitleIncludes => new IdentificationSpec(
            IdentificationStrategy.TitleIncludes,
            TitleParts: rule.TitleRules?.ToMessageParts()),

        IdentificationStrategy.AirdateExtraction => new IdentificationSpec(
            IdentificationStrategy.AirdateExtraction),

        _ => null,
    };

    private static TitlePart[]? ToMessageParts(this TitleRuleInput[] titleRules)
    {
        if (titleRules.Length == 0)
        {
            return null;
        }

        var parts = titleRules
            .Select(tr => new TitlePart(tr.Type, Value: tr.Value, Pattern: tr.Pattern, Field: tr.Field, CaptureGroup: tr.CaptureGroup))
            .ToArray();

        return parts.Length > 0 ? parts : null;
    }

    private static FilterSpec? ToMessage(this FilterGroupInput spec)
    {
        var all = spec.All is not null ? ToMessageNodes(spec.All) : null;
        var any = spec.Any is not null ? ToMessageNodes(spec.Any) : null;
        var not = spec.Not is not null ? ToMessageNodes(spec.Not) : null;

        return all is null && any is null && not is null ? null : new FilterSpec(all, any, not);
    }

    private static FilterNode[]? ToMessageNodes(FilterNodeInput[] nodes)
    {
        var result = new List<FilterNode>();

        foreach (var node in nodes)
        {
            if (node.All is not null || node.Any is not null || node.Not is not null)
            {
                var nested = new FilterGroupInput(node.All, node.Any, node.Not).ToMessage();
                if (nested is not null)
                {
                    result.Add(new FilterNode.GroupNode(nested));
                }
            }
            else if (node.Field is not null && node.Op is not null && node.Value is not null)
            {
                result.Add(new FilterNode.ConditionNode(new FilterCondition(node.Field.Value, node.Op.Value, node.Value)));
            }
        }

        return result.Count > 0 ? result.ToArray() : null;
    }

    private static ScoreCandidate ToMessage(this TestCandidate c) =>
        new(c.Title, c.Topic, c.Channel, c.Duration, c.Quality, c.Description, c.Timestamp);
}
