using System.Diagnostics.CodeAnalysis;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed;

internal sealed record RuleSetGroup(string RuleSetId, MediaType MediaType, ExternalIds Ids, MediathekItem[] Items);

internal sealed record RuleSetTarget(string RuleSetId, MediaType MediaType, ExternalIds Ids);

internal sealed class RuleSetLookup
{
    private readonly Dictionary<string, RuleSetTarget> _byKey;

    private RuleSetLookup(Dictionary<string, RuleSetTarget> byKey)
    {
        _byKey = byKey;
    }

    internal static readonly RuleSetLookup Empty = From([]);

    internal static RuleSetLookup From(IEnumerable<RegisteredRuleSetEntry> entries)
    {
        var byKey = new Dictionary<string, RuleSetTarget>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var value = new RuleSetTarget(entry.RuleSetId, entry.MediaType ?? MediaType.Show, entry.Ids);

            foreach (var key in entry.Aliases.Prepend(entry.Topic))
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    byKey.TryAdd(key.Trim(), value);
                }
            }
        }

        return new RuleSetLookup(byKey);
    }

    internal bool TryMatch(string topic, out string ruleSetId, out MediaType mediaType)
    {
        if (TryMatch(topic, out var target))
        {
            (ruleSetId, mediaType) = (target.RuleSetId, target.MediaType);
            return true;
        }

        ruleSetId = "";
        mediaType = MediaType.Show;
        return false;
    }

    private bool TryMatch(string topic, [NotNullWhen(true)] out RuleSetTarget? target) =>
        _byKey.TryGetValue(topic.Trim(), out target);

    internal RuleSetTarget? Match(string topic) => TryMatch(topic, out var target) ? target : null;
}
