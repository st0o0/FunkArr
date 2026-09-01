using System.Collections.Immutable;
using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet;

public sealed record RuleSetResolverState(
    ImmutableDictionary<string, string> LookupIndex,
    ImmutableDictionary<string, ImmutableHashSet<string>> EntriesByRuleSetId)
{
    public static readonly RuleSetResolverState Empty = new(
        ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
        ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
}

public static class RuleSetResolverStateExtensions
{
    public static RuleSetResolverState Apply(this RuleSetResolverState state, RegisterRuleSet msg)
    {
        var lookupIndex = state.LookupIndex;

        if (state.EntriesByRuleSetId.TryGetValue(msg.RuleSetId, out var previousKeys))
        {
            foreach (var key in previousKeys)
            {
                lookupIndex = lookupIndex.Remove(key);
            }
        }

        var keys = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, msg.Topic);
        lookupIndex = lookupIndex.SetItem(msg.Topic, msg.RuleSetId);

        foreach (var alias in msg.Aliases)
        {
            lookupIndex = lookupIndex.SetItem(alias, msg.RuleSetId);
            keys = keys.Add(alias);
        }

        return new RuleSetResolverState(
            lookupIndex,
            state.EntriesByRuleSetId.SetItem(msg.RuleSetId, keys));
    }

    public static object Resolve(this RuleSetResolverState state, ResolveRuleSet msg)
    {
        return state.LookupIndex.TryGetValue(msg.TopicOrAlias, out var ruleSetId)
            ? new RuleSetResolved(ruleSetId)
            : new RuleSetNotFound(msg.TopicOrAlias);
    }
}
