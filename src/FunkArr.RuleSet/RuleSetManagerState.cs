using System.Collections.Immutable;
using Akka.Event;
using FunkArr.Core;
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
    public static RuleSetDetailResult? BuildDetail(this RuleSetManagerState state, string ruleSetId, IDataFiles dataFiles)
    {
        if (!state.KnownRuleSets.TryGetValue(ruleSetId, out var paths))
        {
            return null;
        }

        var communityExists = paths.CommunityPath is not null && dataFiles.Exists(paths.CommunityPath);
        var localExists = paths.LocalPath is not null && dataFiles.Exists(paths.LocalPath);

        if (!communityExists && !localExists)
        {
            return null;
        }

        var communityJson = communityExists ? dataFiles.ReadText(paths.CommunityPath!) : null;
        var localJson = localExists ? dataFiles.ReadText(paths.LocalPath!) : null;

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
            r.Identification.Strategy,
            r.Identification.SeasonPattern,
            r.Identification.EpisodePattern,
            r.Identification.CaptureGroup,
            MapFilters(r.Filters),
            MapTitleRules(r.Identification.TitleParts)
        )).ToArray();
    }

    private static FilterGroupOutput? MapFilters(FilterSpec? spec)
    {
        if (spec is null)
        {
            return null;
        }

        var all = MapConditions(spec.All);
        var any = MapConditions(spec.Any);
        var not = MapConditions(spec.Not);

        return all is null && any is null && not is null ? null : new FilterGroupOutput(all, any, not);
    }

    private static FilterConditionOutput[]? MapConditions(FilterNode[]? nodes)
    {
        if (nodes is null)
        {
            return null;
        }

        var results = nodes
            .OfType<FilterNode.ConditionNode>()
            .Select(c => new FilterConditionOutput(c.Condition.Field, c.Condition.Op, c.Condition.Value))
            .ToArray();

        return results.Length > 0 ? results : null;
    }

    private static TitleRuleOutput[]? MapTitleRules(TitlePart[]? parts)
    {
        if (parts is null or { Length: 0 })
        {
            return null;
        }

        return parts
            .Select(p => new TitleRuleOutput(p.Type, p.Field, p.Pattern, p.CaptureGroup, p.Value))
            .ToArray();
    }

    public static RuleSetSummaryResult ToSummaries(this RuleSetManagerState state, IDataFiles dataFiles, ILoggingAdapter? log = null)
    {
        var entries = new List<RuleSetSummaryEntry>();

        foreach (var (ruleSetId, paths) in state.KnownRuleSets)
        {
            var sourceType = (paths.CommunityPath, paths.LocalPath) switch
            {
                (not null, not null) => "merged",
                (not null, null) => "community",
                (null, not null) => "local",
                _ => "unknown",
            };

            var ruleCount = 0;
            try
            {
                var communityJson = paths.CommunityPath is not null && dataFiles.Exists(paths.CommunityPath)
                    ? dataFiles.ReadText(paths.CommunityPath) : null;
                var localJson = paths.LocalPath is not null && dataFiles.Exists(paths.LocalPath)
                    ? dataFiles.ReadText(paths.LocalPath) : null;

                var config = RuleSetMerger.Build(ruleSetId, communityJson, localJson);
                if (config is not null)
                {
                    ruleCount = config.Rules.Length;
                }
            }
            catch (Exception ex)
            {
                if (log is not null)
                    log.Warning("Failed to load ruleset config for {RuleSetId}: {Error}", ruleSetId, ex.Message);
            }

            entries.Add(new RuleSetSummaryEntry(ruleSetId, ruleCount, sourceType));
        }

        return new RuleSetSummaryResult(entries.ToArray());
    }

    public static RuleSetPaths CheckRuleSetPaths(string ruleSetId, string communityDir, string localDir, IDataFiles dataFiles)
    {
        var communityPath = Path.Combine(communityDir, $"{ruleSetId}.json");
        var localPath = Path.Combine(localDir, $"{ruleSetId}.json");

        var communityExists = dataFiles.Exists(communityPath);
        var localExists = dataFiles.Exists(localPath);

        return new RuleSetPaths(
            communityExists ? communityPath : null,
            localExists ? localPath : null,
            communityExists ? File.GetLastWriteTimeUtc(communityPath) : null,
            localExists ? File.GetLastWriteTimeUtc(localPath) : null);
    }
}
