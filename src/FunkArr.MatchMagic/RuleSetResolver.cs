namespace FunkArr.MatchMagic;

public static class RuleSetResolver
{
    public static RuleSet? Resolve(RuleSet? community, RuleSet? local)
    {
        if (community is null && local is null)
            return null;

        if (community is null)
            return local;

        if (local is null)
            return community;

        if (local.Standalone)
            return local;

        return Merge(community, local);
    }

    private static RuleSet Merge(RuleSet community, RuleSet local)
    {
        var rules = MergeRules(community.EffectiveRules, local.EffectiveRules, local.Disable);
        var aliases = MergeAliases(community.Aliases, local.Aliases);
        var confidence = local.Confidence ?? community.Confidence;
        var media = local.Media ?? community.Media;

        return new RuleSet(
            Topic: community.Topic,
            Aliases: aliases,
            Media: media,
            Confidence: confidence,
            Rules: rules);
    }

    private static IReadOnlyList<Rule> MergeRules(
        IReadOnlyList<Rule> communityRules,
        IReadOnlyList<Rule> localRules,
        IReadOnlyList<string>? disable)
    {
        var disabledIds = disable is { Count: > 0 }
            ? new HashSet<string>(disable, StringComparer.Ordinal)
            : null;

        var localById = new Dictionary<string, Rule>(StringComparer.Ordinal);
        foreach (var rule in localRules)
            localById[rule.Id] = rule;

        var merged = new List<Rule>();

        foreach (var rule in communityRules)
        {
            if (disabledIds is not null && disabledIds.Contains(rule.Id))
                continue;

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
            if (localById.ContainsKey(rule.Id))
                merged.Add(rule);
        }

        merged.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        return merged;
    }

    private static IReadOnlyList<string>? MergeAliases(
        IReadOnlyList<string>? community,
        IReadOnlyList<string>? local)
    {
        if (community is null or { Count: 0 } && local is null or { Count: 0 })
            return null;

        var set = new HashSet<string>(StringComparer.Ordinal);

        if (community is { Count: > 0 })
            foreach (var alias in community)
                set.Add(alias);

        if (local is { Count: > 0 })
            foreach (var alias in local)
                set.Add(alias);

        return set.ToList();
    }
}
