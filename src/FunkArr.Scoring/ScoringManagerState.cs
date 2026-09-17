using System.Collections.Immutable;
using FunkArr.Messages.Scoring;

namespace FunkArr.Scoring;

public sealed record ScoringManagerState(
    ImmutableDictionary<string, MatchingConfig> Configs)
{
    public static readonly ScoringManagerState Empty =
        new(ImmutableDictionary<string, MatchingConfig>.Empty.WithComparers(StringComparer.Ordinal));
}

public static class ScoringManagerStateExtensions
{
    public static ScoringManagerState Apply(this ScoringManagerState state, MatchingConfig config) =>
        state with { Configs = state.Configs.SetItem(config.RuleSetId, config) };

    public static ScoringManagerState Apply(this ScoringManagerState state, RemoveMatchingConfig msg) =>
        state with { Configs = state.Configs.Remove(msg.RuleSetId) };

    public static MatchingConfig? GetConfig(this ScoringManagerState state, string ruleSetId) =>
        state.Configs.TryGetValue(ruleSetId, out var config) ? config : null;
}
