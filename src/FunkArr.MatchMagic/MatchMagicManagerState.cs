using System.Collections.Immutable;
using FunkArr.Messages.Scoring;

namespace FunkArr.MatchMagic;

public sealed record MatchMagicManagerState(
    ImmutableDictionary<string, MatchingConfig> Configs)
{
    public static readonly MatchMagicManagerState Empty =
        new(ImmutableDictionary<string, MatchingConfig>.Empty.WithComparers(StringComparer.Ordinal));
}

public static class MatchMagicManagerStateExtensions
{
    public static MatchMagicManagerState Apply(this MatchMagicManagerState state, MatchingConfig config) =>
        state with { Configs = state.Configs.SetItem(config.RuleSetId, config) };

    public static MatchMagicManagerState Apply(this MatchMagicManagerState state, RemoveMatchingConfig msg) =>
        state with { Configs = state.Configs.Remove(msg.RuleSetId) };

    public static MatchingConfig? GetConfig(this MatchMagicManagerState state, string ruleSetId) =>
        state.Configs.TryGetValue(ruleSetId, out var config) ? config : null;
}
