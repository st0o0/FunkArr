using FunkArr.RuleSet.DiskModel;

namespace FunkArr.RuleSet;

internal static class RuleSetMerger
{
    public static DiskRuleSet? Resolve(DiskRuleSet? community, DiskRuleSet? local)
    {
        if (community is null && local is null) return null;
        if (community is null) return local;
        if (local is null) return community;
        if (local.Standalone) return local;
        return Merge(community, local);
    }

    private static DiskRuleSet Merge(DiskRuleSet community, DiskRuleSet local) => new()
    {
        Topic = community.Topic,
        Aliases = MergeAliases(community.Aliases, local.Aliases),
        Media = MergeMedia(community.Media, local.Media),
        Confidence = local.Confidence ?? community.Confidence,
        Rules = MergeRules(community.Rules ?? [], local.Rules ?? [], local.Disable),
        Enrichment = MergeEnrichment(community.Enrichment, local.Enrichment),
    };

    private static DiskMedia? MergeMedia(DiskMedia? community, DiskMedia? local)
    {
        if (community is null && local is null) return null;
        if (community is null) return local;
        if (local is null) return community;
        return new DiskMedia
        {
            TvdbId = local.TvdbId ?? community.TvdbId,
            ImdbId = local.ImdbId ?? community.ImdbId,
            TmdbId = local.TmdbId ?? community.TmdbId,
            Name = local.Name ?? community.Name,
            Type = local.Type ?? community.Type,
        };
    }

    private static List<DiskRule> MergeRules(List<DiskRule> communityRules, List<DiskRule> localRules, List<string>? disable)
    {
        var disabledIds = disable is { Count: > 0 }
            ? new HashSet<string>(disable, StringComparer.Ordinal)
            : null;

        var localById = new Dictionary<string, DiskRule>(StringComparer.Ordinal);
        foreach (var rule in localRules) localById[rule.Id] = rule;

        var merged = new List<DiskRule>();

        foreach (var rule in communityRules)
        {
            if (disabledIds is not null && disabledIds.Contains(rule.Id)) continue;
            if (localById.TryGetValue(rule.Id, out var replacement))
            {
                merged.Add(replacement);
                localById.Remove(rule.Id);
            }
            else
            {
                merged.Add(rule);
            }
        }

        foreach (var rule in localRules)
        {
            if (localById.ContainsKey(rule.Id)) merged.Add(rule);
        }

        merged.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        return merged;
    }

    private static List<string>? MergeAliases(List<string>? community, List<string>? local)
    {
        if (community is null or { Count: 0 } && local is null or { Count: 0 }) return null;

        var set = new HashSet<string>(StringComparer.Ordinal);
        if (community is { Count: > 0 }) foreach (var alias in community) set.Add(alias);
        if (local is { Count: > 0 }) foreach (var alias in local) set.Add(alias);
        return set.ToList();
    }

    private static DiskEnrichment? MergeEnrichment(DiskEnrichment? community, DiskEnrichment? local)
    {
        if (community is null && local is null) return null;
        if (community is null) return local;
        if (local is null) return community;

        return new DiskEnrichment
        {
            Enabled = local.Enabled ?? community.Enabled,
            Methods = local.Methods ?? community.Methods,
            Title = local.Title is not null
                ? new DiskTitleMatch { Threshold = local.Title.Threshold ?? community.Title?.Threshold }
                : community.Title,
            Airdate = local.Airdate is not null
                ? new DiskAirdateMatch
                {
                    Tolerance = local.Airdate.Tolerance ?? community.Airdate?.Tolerance,
                    MinTitleAffinity = local.Airdate.MinTitleAffinity ?? community.Airdate?.MinTitleAffinity,
                }
                : community.Airdate,
            Runtime = local.Runtime is not null
                ? new DiskRuntimeMatch
                {
                    Tolerance = local.Runtime.Tolerance ?? community.Runtime?.Tolerance,
                    Mode = local.Runtime.Mode ?? community.Runtime?.Mode,
                }
                : community.Runtime,
            Year = local.Year is not null
                ? new DiskYearMatch { Tolerance = local.Year.Tolerance ?? community.Year?.Tolerance }
                : community.Year,
        };
    }
}
