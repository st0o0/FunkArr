using System.Collections.Immutable;
using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet;

public sealed record RuleSetResolverState(
    ImmutableDictionary<string, string> LookupIndex,
    ImmutableDictionary<string, ImmutableHashSet<string>> EntriesByRuleSetId,
    ImmutableDictionary<string, string> IdIndex,
    ImmutableDictionary<string, string> TopicByRuleSetId)
{
    public static readonly RuleSetResolverState Empty = new(
        ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
        ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
        ImmutableDictionary<string, string>.Empty,
        ImmutableDictionary<string, string>.Empty);
}

public static class RuleSetResolverStateExtensions
{
    public static RuleSetResolverState Apply(this RuleSetResolverState state, DeregisterRuleSet msg)
    {
        if (!state.EntriesByRuleSetId.TryGetValue(msg.RuleSetId, out var keys))
        {
            return state;
        }

        var lookupIndex = state.LookupIndex;
        foreach (var key in keys)
        {
            lookupIndex = lookupIndex.Remove(key);
        }

        var idIndex = RemoveIdEntries(state.IdIndex, msg.RuleSetId);

        return new RuleSetResolverState(
            lookupIndex,
            state.EntriesByRuleSetId.Remove(msg.RuleSetId),
            idIndex,
            state.TopicByRuleSetId.Remove(msg.RuleSetId));
    }

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

        var idIndex = RemoveIdEntries(state.IdIndex, msg.RuleSetId);
        idIndex = AddIdEntry(idIndex, "tvdb", msg.TvdbId?.ToString(), msg.RuleSetId);
        idIndex = AddIdEntry(idIndex, "imdb", msg.ImdbId, msg.RuleSetId);
        idIndex = AddIdEntry(idIndex, "tmdb", msg.TmdbId?.ToString(), msg.RuleSetId);

        return new RuleSetResolverState(
            lookupIndex,
            state.EntriesByRuleSetId.SetItem(msg.RuleSetId, keys),
            idIndex,
            state.TopicByRuleSetId.SetItem(msg.RuleSetId, msg.Topic));
    }

    public static object Resolve(this RuleSetResolverState state, ResolveRuleSet msg)
    {
        if (msg.TopicOrAlias is not null &&
            state.LookupIndex.TryGetValue(msg.TopicOrAlias, out var ruleSetId))
        {
            var topic = state.TopicByRuleSetId.GetValueOrDefault(ruleSetId, msg.TopicOrAlias);
            return new RuleSetResolved(ruleSetId, topic);
        }

        if (TryResolveById(state, "tvdb", msg.TvdbId?.ToString(), out var byTvdb))
        {
            return byTvdb;
        }

        if (TryResolveById(state, "imdb", msg.ImdbId, out var byImdb))
        {
            return byImdb;
        }

        if (TryResolveById(state, "tmdb", msg.TmdbId?.ToString(), out var byTmdb))
        {
            return byTmdb;
        }

        return new RuleSetNotFound(msg.TopicOrAlias ?? "");
    }

    private static bool TryResolveById(
        RuleSetResolverState state, string prefix, string? value, out RuleSetResolved resolved)
    {
        resolved = default!;
        if (value is null)
        {
            return false;
        }

        var key = $"{prefix}:{value}";
        if (!state.IdIndex.TryGetValue(key, out var ruleSetId))
        {
            return false;
        }

        var topic = state.TopicByRuleSetId.GetValueOrDefault(ruleSetId, "");
        resolved = new RuleSetResolved(ruleSetId, topic);
        return true;
    }

    private static ImmutableDictionary<string, string> RemoveIdEntries(
        ImmutableDictionary<string, string> idIndex, string ruleSetId)
    {
        var toRemove = idIndex.Where(kv => kv.Value == ruleSetId).Select(kv => kv.Key).ToList();
        foreach (var key in toRemove)
        {
            idIndex = idIndex.Remove(key);
        }

        return idIndex;
    }

    private static ImmutableDictionary<string, string> AddIdEntry(ImmutableDictionary<string, string> idIndex,
        string prefix, string? value, string ruleSetId)
        => value is not null ? idIndex.SetItem($"{prefix}:{value}", ruleSetId) : idIndex;

    public static RegisteredRuleSetsResult QueryAll(this RuleSetResolverState state)
    {
        var entries = new List<RegisteredRuleSetEntry>();

        foreach (var (ruleSetId, keys) in state.EntriesByRuleSetId)
        {
            var topic = state.TopicByRuleSetId.GetValueOrDefault(ruleSetId, "");
            var aliases = keys.Where(k => !string.Equals(k, topic, StringComparison.OrdinalIgnoreCase)).ToArray();

            int? tvdbId = null;
            string? imdbId = null;
            int? tmdbId = null;

            foreach (var (key, id) in state.IdIndex)
            {
                if (id != ruleSetId)
                {
                    continue;
                }

                if (key.StartsWith("tvdb:") && int.TryParse(key[5..], out var tvdb))
                {
                    tvdbId = tvdb;
                }
                else if (key.StartsWith("imdb:"))
                {
                    imdbId = key[5..];
                }
                else if (key.StartsWith("tmdb:") && int.TryParse(key[5..], out var tmdb))
                {
                    tmdbId = tmdb;
                }
            }

            entries.Add(new RegisteredRuleSetEntry(ruleSetId, topic, aliases, tvdbId, imdbId, tmdbId));
        }

        return new RegisteredRuleSetsResult(entries.ToArray());
    }
}
