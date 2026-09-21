using System.Collections.Immutable;
using FunkArr.Messages.Scoring;

namespace FunkArr.Scoring;

public sealed record ScoringManagerState(
    ImmutableDictionary<string, MatchingConfig> Configs)
{
    public static readonly ScoringManagerState Empty =
        new(ImmutableDictionary<string, MatchingConfig>.Empty.WithComparers(StringComparer.Ordinal));

    public static ScoringManagerState FromSnapshot(ScoringManagerSnapshot snapshot) =>
        new(snapshot.Configs.WithComparers(StringComparer.Ordinal));
}

public sealed record ScoringManagerSnapshot(
    ImmutableDictionary<string, MatchingConfig> Configs);

public static class ScoringManagerStateExtensions
{
    public static ScoringManagerState Apply(this ScoringManagerState state, MatchingConfig config) =>
        new(Configs: state.Configs.SetItem(config.RuleSetId, config));

    public static ScoringManagerState Apply(this ScoringManagerState state, RemoveMatchingConfig msg) =>
        new(Configs: state.Configs.Remove(msg.RuleSetId));

    public static MatchingConfig? GetConfig(this ScoringManagerState state, string ruleSetId) =>
        state.Configs.GetValueOrDefault(ruleSetId);

    public static ScoringManagerSnapshot GetSnapshot(this ScoringManagerState state) =>
        new(state.Configs);
}
