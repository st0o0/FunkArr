using FunkArr.Messages.Scoring;

namespace FunkArr.Scoring;

public sealed class ScoringManagerState
{
    public Dictionary<string, MatchingConfig> Configs { get; } = new(StringComparer.Ordinal);

    public static ScoringManagerState Empty => new();

    public void Apply(MatchingConfig config) => Configs[config.RuleSetId] = config;

    public void Apply(RemoveMatchingConfig msg) => Configs.Remove(msg.RuleSetId);

    public MatchingConfig? GetConfig(string ruleSetId) =>
        Configs.GetValueOrDefault(ruleSetId);
}
