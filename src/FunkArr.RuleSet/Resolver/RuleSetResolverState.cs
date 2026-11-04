using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.RuleSet.Resolver;

public sealed class RuleSetResolverState
{
    public Dictionary<string, string> LookupIndex { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, HashSet<string>> EntriesByRuleSetId { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> IdIndex { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> TopicByRuleSetId { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> MediaNameByRuleSetId { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, MediaType> MediaTypeByRuleSetId { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, EnrichmentConfig> EnrichmentConfigByRuleSetId { get; } = new(StringComparer.Ordinal);

    public void Apply(DeregisterRuleSet msg)
    {
        if (!EntriesByRuleSetId.TryGetValue(msg.RuleSetId, out var keys))
        {
            return;
        }

        foreach (var key in keys)
        {
            LookupIndex.Remove(key);
        }

        RemoveIdEntries(msg.RuleSetId);

        EntriesByRuleSetId.Remove(msg.RuleSetId);
        TopicByRuleSetId.Remove(msg.RuleSetId);
        MediaNameByRuleSetId.Remove(msg.RuleSetId);
        MediaTypeByRuleSetId.Remove(msg.RuleSetId);
        EnrichmentConfigByRuleSetId.Remove(msg.RuleSetId);
    }

    public void Apply(RegisterRuleSet msg)
    {
        if (EntriesByRuleSetId.TryGetValue(msg.RuleSetId, out var previousKeys))
        {
            foreach (var key in previousKeys)
            {
                LookupIndex.Remove(key);
            }
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { msg.Topic };
        LookupIndex[msg.Topic] = msg.RuleSetId;

        foreach (var alias in msg.Aliases)
        {
            LookupIndex[alias] = msg.RuleSetId;
            keys.Add(alias);
        }

        RemoveIdEntries(msg.RuleSetId);
        AddIdEntry("tvdb", msg.Ids?.TvdbId?.ToString(), msg.RuleSetId);
        AddIdEntry("imdb", msg.Ids?.ImdbId, msg.RuleSetId);
        AddIdEntry("tmdb", msg.Ids?.TmdbId?.ToString(), msg.RuleSetId);

        if (msg.MediaName is not null)
        {
            MediaNameByRuleSetId[msg.RuleSetId] = msg.MediaName;
        }
        else
        {
            MediaNameByRuleSetId.Remove(msg.RuleSetId);
        }

        if (msg.MediaType is not null)
        {
            MediaTypeByRuleSetId[msg.RuleSetId] = msg.MediaType.Value;
        }
        else
        {
            MediaTypeByRuleSetId.Remove(msg.RuleSetId);
        }

        if (msg.Enrichment is not null)
        {
            EnrichmentConfigByRuleSetId[msg.RuleSetId] = msg.Enrichment;
        }
        else
        {
            EnrichmentConfigByRuleSetId.Remove(msg.RuleSetId);
        }

        EntriesByRuleSetId[msg.RuleSetId] = keys;
        TopicByRuleSetId[msg.RuleSetId] = msg.Topic;
    }

    public ResolveRuleSetResponse Resolve(ResolveRuleSet msg)
    {
        if (msg.TopicOrAlias is not null &&
            LookupIndex.TryGetValue(msg.TopicOrAlias, out var ruleSetId))
        {
            var topic = TopicByRuleSetId.GetValueOrDefault(ruleSetId, msg.TopicOrAlias);
            var mediaName = MediaNameByRuleSetId.GetValueOrDefault(ruleSetId);
            var enrichment = EnrichmentConfigByRuleSetId.GetValueOrDefault(ruleSetId);
            return new RuleSetResolved(ruleSetId, topic, mediaName, enrichment);
        }

        if (TryResolveById("tvdb", msg.Ids?.TvdbId?.ToString(), out var byTvdb))
        {
            return byTvdb;
        }

        if (TryResolveById("imdb", msg.Ids?.ImdbId, out var byImdb))
        {
            return byImdb;
        }

        if (TryResolveById("tmdb", msg.Ids?.TmdbId?.ToString(), out var byTmdb))
        {
            return byTmdb;
        }

        return new RuleSetFailed(new RuleSetNotFoundException(msg.TopicOrAlias ?? ""));
    }

    private bool TryResolveById(string prefix, string? value, out RuleSetResolved resolved)
    {
        resolved = null!;
        if (value is null)
        {
            return false;
        }

        var key = $"{prefix}:{value}";
        if (!IdIndex.TryGetValue(key, out var ruleSetId))
        {
            return false;
        }

        var topic = TopicByRuleSetId.GetValueOrDefault(ruleSetId, "");
        var mediaName = MediaNameByRuleSetId.GetValueOrDefault(ruleSetId);
        var enrichment = EnrichmentConfigByRuleSetId.GetValueOrDefault(ruleSetId);
        resolved = new RuleSetResolved(ruleSetId, topic, mediaName, enrichment);
        return true;
    }

    private void RemoveIdEntries(string ruleSetId)
    {
        var toRemove = IdIndex.Where(kv => kv.Value == ruleSetId).Select(kv => kv.Key).ToList();
        foreach (var key in toRemove)
        {
            IdIndex.Remove(key);
        }
    }

    private void AddIdEntry(string prefix, string? value, string ruleSetId)
    {
        if (value is not null)
        {
            IdIndex[$"{prefix}:{value}"] = ruleSetId;
        }
    }

    public RegisteredRuleSetsResult GetSnapshot() => QueryAll();

    public static RuleSetResolverState FromSnapshot(RegisteredRuleSetsResult snapshot)
    {
        var state = new RuleSetResolverState();
        foreach (var entry in snapshot.Entries)
        {
            state.Apply(new RegisterRuleSet(
                entry.RuleSetId, entry.Topic, entry.Aliases,
                entry.Ids,
                entry.MediaName, entry.MediaType));
        }

        return state;
    }

    public RegisteredRuleSetsResult QueryAll()
    {
        var entries = new List<RegisteredRuleSetEntry>();

        foreach (var (ruleSetId, keys) in EntriesByRuleSetId)
        {
            var topic = TopicByRuleSetId.GetValueOrDefault(ruleSetId, "");
            var aliases = keys.Where(k => !string.Equals(k, topic, StringComparison.OrdinalIgnoreCase)).ToArray();

            int? tvdbId = null;
            string? imdbId = null;
            int? tmdbId = null;

            foreach (var (key, id) in IdIndex)
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

            var mediaName = MediaNameByRuleSetId.GetValueOrDefault(ruleSetId);
            MediaType? mediaType = MediaTypeByRuleSetId.TryGetValue(ruleSetId, out var mt) ? mt : null;
            entries.Add(new RegisteredRuleSetEntry(ruleSetId, topic, aliases, new ExternalIds(tvdbId, imdbId, tmdbId), mediaName, mediaType));
        }

        return new RegisteredRuleSetsResult([.. entries]);
    }
}
