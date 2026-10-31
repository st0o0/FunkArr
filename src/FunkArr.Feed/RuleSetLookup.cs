using System.Diagnostics.CodeAnalysis;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed;

internal sealed record RuleSetGroup(string RuleSetId, MediaType MediaType, ExternalIds Ids, MediathekItem[] Items);

internal sealed record RuleSetTarget(string RuleSetId, MediaType MediaType, ExternalIds Ids);

internal sealed record GroupingResult(RuleSetGroup[] Groups, int Unmatched);

internal sealed class RuleSetLookup
{
    private readonly Dictionary<string, RuleSetTarget> _byKey;

    private RuleSetLookup(Dictionary<string, RuleSetTarget> byKey)
    {
        _byKey = byKey;
    }

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
        if (TryMatch(topic, out RuleSetTarget? target))
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

    internal GroupingResult Group(IEnumerable<MediathekItem> items)
    {
        var groups = new Dictionary<string, (RuleSetTarget Target, List<MediathekItem> Items)>(StringComparer.Ordinal);
        var unmatched = 0;

        foreach (var item in items)
        {
            if (!TryMatch(item.Topic, out RuleSetTarget? target))
            {
                unmatched++;
                continue;
            }

            if (!groups.TryGetValue(target.RuleSetId, out var group))
            {
                group = (target, []);
                groups[target.RuleSetId] = group;
            }

            group.Items.Add(item);
        }

        return new GroupingResult(
            [.. groups.Select(g => new RuleSetGroup(g.Key, g.Value.Target.MediaType, g.Value.Target.Ids, [.. g.Value.Items]))],
            unmatched);
    }
}
