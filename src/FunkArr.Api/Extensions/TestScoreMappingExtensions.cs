using FunkArr.Api.Models;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Extensions;

internal static class TestScoreMappingExtensions
{
    internal static (MsgScoring.MatchingConfig Config, MsgScoring.ScoreCandidate[] Candidates) ToMessage(this TestScoreRequest request)
    {
        var rules = (request.Rules)
            .Select(r => r.ToMessage())
            .Where(r => r is not null)
            .Select(r => r!)
            .ToArray();

        var config = new MsgScoring.MatchingConfig("test", request.DefaultConfidence, rules);
        var candidates = (request.Candidates).Select(c => c.ToMessage()).ToArray();

        return (config, candidates);
    }

    internal static MsgScoring.MatchingRule? ToMessage(this RuleInput rule)
    {
        var identification = rule.ToIdentification();
        if (identification is null)
        {
            return null;
        }

        var filters = rule.Filters?.ToMessage();

        return new MsgScoring.MatchingRule(rule.Id, rule.Priority, rule.Confidence, filters, identification);
    }

    private static MsgScoring.IdentificationSpec? ToIdentification(this RuleInput rule)
    {
        var strategy = rule.Strategy?.ToMessage();

        return strategy switch
        {
            MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber => new MsgScoring.IdentificationSpec(
                MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: rule.SeasonRegex,
                EpisodePattern: rule.EpisodeRegex,
                CaptureGroup: rule.CaptureGroup),

            MsgScoring.IdentificationStrategy.AbsoluteEpisodeNumber => new MsgScoring.IdentificationSpec(
                MsgScoring.IdentificationStrategy.AbsoluteEpisodeNumber,
                EpisodePattern: rule.EpisodeRegex,
                CaptureGroup: rule.CaptureGroup),

            MsgScoring.IdentificationStrategy.TitleExact => new MsgScoring.IdentificationSpec(
                MsgScoring.IdentificationStrategy.TitleExact,
                TitleParts: rule.TitleRules?.ToMessageParts()),

            MsgScoring.IdentificationStrategy.TitleIncludes => new MsgScoring.IdentificationSpec(
                MsgScoring.IdentificationStrategy.TitleIncludes,
                TitleParts: rule.TitleRules?.ToMessageParts()),

            MsgScoring.IdentificationStrategy.AirdateExtraction => new MsgScoring.IdentificationSpec(
                MsgScoring.IdentificationStrategy.AirdateExtraction),

            _ => null
        };
    }

    private static MsgScoring.TitlePart[]? ToMessageParts(this TitleRuleInput[] titleRules)
    {
        if (titleRules.Length == 0)
        {
            return null;
        }

        var parts = titleRules
            .Select(tr => new MsgScoring.TitlePart(
                tr.Type.ToMessage(),
                Value: tr.Value,
                Pattern: tr.Pattern,
                Field: tr.Field?.ToMessage(),
                CaptureGroup: tr.CaptureGroup))
            .ToArray();

        return parts.Length > 0 ? parts : null;
    }

    private static MsgScoring.FilterSpec? ToMessage(this FilterGroupInput spec)
    {
        var all = spec.All is not null ? ToMessageNodes(spec.All) : null;
        var any = spec.Any is not null ? ToMessageNodes(spec.Any) : null;
        var not = spec.Not is not null ? ToMessageNodes(spec.Not) : null;

        return all is null && any is null && not is null ? null : new MsgScoring.FilterSpec(all, any, not);
    }

    private static MsgScoring.FilterNode[]? ToMessageNodes(FilterNodeInput[] nodes)
    {
        var result = new List<MsgScoring.FilterNode>();

        foreach (var node in nodes)
        {
            if (node.All is not null || node.Any is not null || node.Not is not null)
            {
                var nested = new FilterGroupInput(node.All, node.Any, node.Not).ToMessage();
                if (nested is not null)
                {
                    result.Add(new MsgScoring.FilterNode.GroupNode(nested));
                }
            }
            else if (node.Field is not null && node.Op is not null && node.Value is not null)
            {
                result.Add(new MsgScoring.FilterNode.ConditionNode(
                    new MsgScoring.FilterCondition(
                        node.Field.Value.ToMessage(),
                        node.Op.Value.ToMessage(),
                        node.Value)));
            }
        }

        return result.Count > 0 ? [.. result] : null;
    }

    private static MsgScoring.ScoreCandidate ToMessage(this TestCandidate c) =>
        new(c.Title, c.Topic, c.Channel, c.Duration, c.Quality, c.Description, c.Timestamp);
}
