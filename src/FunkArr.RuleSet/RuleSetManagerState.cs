using System.Collections.Immutable;
using System.Text;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;

namespace FunkArr.RuleSet;

public sealed record RuleSetManagerState(
    ImmutableDictionary<string, RuleSetPaths> KnownRuleSets,
    ImmutableHashSet<string> PendingIds,
    bool FullRescanRequested)
{
    public static readonly RuleSetManagerState Empty = new(
        ImmutableDictionary<string, RuleSetPaths>.Empty.WithComparers(StringComparer.Ordinal),
        ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
        false);
}

public sealed record RuleSetPaths(
    string? CommunityPath,
    string? LocalPath,
    DateTime? CommunityModified,
    DateTime? LocalModified);

public static class RuleSetManagerStateExtensions
{
    public static RuleSetDetailResult? BuildDetail(this RuleSetManagerState state, string ruleSetId)
    {
        if (!state.KnownRuleSets.TryGetValue(ruleSetId, out var paths))
        {
            return null;
        }

        var communityExists = paths.CommunityPath is not null && File.Exists(paths.CommunityPath);
        var localExists = paths.LocalPath is not null && File.Exists(paths.LocalPath);

        if (!communityExists && !localExists)
        {
            return null;
        }

        var communityJson = communityExists ? File.ReadAllText(paths.CommunityPath!) : null;
        var localJson = localExists ? File.ReadAllText(paths.LocalPath!) : null;

        var identity = RuleSetMerger.ExtractIdentity(communityJson, localJson);
        var config = RuleSetMerger.Build(ruleSetId, communityJson, localJson);

        if (identity is null || config is null)
        {
            return null;
        }

        return new RuleSetDetailResult(
            ruleSetId,
            new RuleSetDetailResult.RuleSetIdentity(
                identity.Value.Topic,
                identity.Value.Aliases,
                identity.Value.TvdbId,
                identity.Value.ImdbId,
                identity.Value.TmdbId),
            new RuleSetDetailResult.RuleSetSource(
                communityExists ? paths.CommunityPath : null,
                localExists ? paths.LocalPath : null,
                paths.CommunityModified,
                paths.LocalModified),
            config.DefaultConfidence,
            config.Rules.ToDetailRules());
    }

    public static RuleSetDetailRule[] ToDetailRules(this MatchingRule[] rules)
    {
        return rules.Select(r => new RuleSetDetailRule(
            r.Id,
            r.Priority,
            r.Confidence,
            r.Identification.Strategy.ToString(),
            SummarizeFilters(r.Filters),
            r.Identification.SeasonPattern,
            r.Identification.EpisodePattern,
            r.Identification.MatchMode?.ToString(),
            r.Identification.TitleParts?.Select(FormatTitlePart).ToArray()
        )).ToArray();
    }

    private static string? SummarizeFilters(FilterSpec? spec)
    {
        if (spec is null)
        {
            return null;
        }

        var sb = new StringBuilder();
        AppendGroup(sb, "all", spec.All);
        AppendGroup(sb, "any", spec.Any);
        AppendGroup(sb, "not", spec.Not);
        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static void AppendGroup(StringBuilder sb, string label, FilterNode[]? nodes)
    {
        if (nodes is null)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append("; ");
        }

        sb.Append(label).Append(": ");
        sb.Append(string.Join(", ", nodes.Select(FormatNode)));
    }

    private static string FormatNode(FilterNode node) => node switch
    {
        FilterNode.ConditionNode c =>
            $"{c.Condition.Field.ToString().ToLowerInvariant()} {c.Condition.Op.ToString().ToLowerInvariant()} '{c.Condition.Value}'",
        FilterNode.GroupNode g => $"({SummarizeFilters(g.Group)})",
        _ => "?",
    };

    private static string FormatTitlePart(TitlePart part) => part.Type switch
    {
        TitlePartType.Static => $"static: '{part.Value}'",
        TitlePartType.Regex => $"regex: {part.Field?.ToString().ToLowerInvariant()} /{part.Pattern}/",
        _ => "?",
    };

    public static RuleSetPaths CheckRuleSetPaths(string ruleSetId, string communityDir, string localDir)
    {
        var communityPath = Path.Combine(communityDir, $"{ruleSetId}.json");
        var localPath = Path.Combine(localDir, $"{ruleSetId}.json");

        var communityExists = File.Exists(communityPath);
        var localExists = File.Exists(localPath);

        return new RuleSetPaths(
            communityExists ? communityPath : null,
            localExists ? localPath : null,
            communityExists ? File.GetLastWriteTimeUtc(communityPath) : null,
            localExists ? File.GetLastWriteTimeUtc(localPath) : null);
    }
}
