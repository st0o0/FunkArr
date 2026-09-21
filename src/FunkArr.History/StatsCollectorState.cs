using System.Collections.Immutable;
using FunkArr.Messages.History;

namespace FunkArr.History;

public sealed record StatsCollectorState(
    ImmutableDictionary<string, ScoringStatsResult> Stats)
{
    public static readonly StatsCollectorState Empty = new(
        ImmutableDictionary<string, ScoringStatsResult>.Empty.WithComparers(StringComparer.Ordinal));

    public static StatsCollectorState FromSnapshot(AllStatsSnapshot snapshot) =>
        new(snapshot.Entries.WithComparers(StringComparer.Ordinal));
}

public static class StatsCollectorStateExtensions
{
    public static StatsCollectorState Apply(this StatsCollectorState state, StatsUpdated msg) =>
        new(state.Stats.SetItem(msg.RuleSetId, msg.Stats));

    public static StatsCollectorState Apply(this StatsCollectorState state, RemoveStats msg) =>
        new(state.Stats.Remove(msg.RuleSetId));

    public static AllStatsSnapshot GetSnapshot(this StatsCollectorState state) =>
        new(state.Stats);
}
