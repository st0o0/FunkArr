using System.Collections.Immutable;
using FunkArr.Messages.History;

namespace FunkArr.History;

public sealed class StatsCollectorState
{
    public Dictionary<string, ScoringStatsResult> Stats { get; } = new(StringComparer.Ordinal);

    public static StatsCollectorState Empty => new();

    public static StatsCollectorState FromSnapshot(AllStatsResult snapshot)
    {
        var state = new StatsCollectorState();
        foreach (var (key, value) in snapshot.Entries)
        {
            state.Stats[key] = value;
        }

        return state;
    }

    public void Apply(UpdateStats msg) => Stats[msg.RuleSetId] = msg.Stats;

    public void Apply(RemoveStats msg) => Stats.Remove(msg.RuleSetId);

    public AllStatsResult GetSnapshot() => new(Stats.ToImmutableDictionary());
}
