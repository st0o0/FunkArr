using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;

namespace FunkArr.Feed;

internal sealed record RuleSetGroup(string RuleSetId, MediaType MediaType, MediathekItem[] Items);

internal sealed record GroupingResult(RuleSetGroup[] Groups, int Unmatched);

internal sealed class RuleSetLookup
{
    private readonly Dictionary<string, (string RuleSetId, MediaType MediaType)> _byKey;

    private RuleSetLookup(Dictionary<string, (string, MediaType)> byKey)
    {
        _byKey = byKey;
    }

    internal static RuleSetLookup From(IEnumerable<RegisteredRuleSetEntry> entries)
    {
        var byKey = new Dictionary<string, (string, MediaType)>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var value = (entry.RuleSetId, entry.MediaType ?? MediaType.Show);

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
        if (_byKey.TryGetValue(topic.Trim(), out var match))
        {
            (ruleSetId, mediaType) = match;
            return true;
        }

        ruleSetId = "";
        mediaType = MediaType.Show;
        return false;
    }

    internal GroupingResult Group(IEnumerable<MediathekItem> items)
    {
        var groups = new Dictionary<string, (MediaType MediaType, List<MediathekItem> Items)>(StringComparer.Ordinal);
        var unmatched = 0;

        foreach (var item in items)
        {
            if (!TryMatch(item.Topic, out var ruleSetId, out var mediaType))
            {
                unmatched++;
                continue;
            }

            if (!groups.TryGetValue(ruleSetId, out var group))
            {
                group = (mediaType, []);
                groups[ruleSetId] = group;
            }

            group.Items.Add(item);
        }

        return new GroupingResult(
            [.. groups.Select(g => new RuleSetGroup(g.Key, g.Value.MediaType, [.. g.Value.Items]))],
            unmatched);
    }
}
