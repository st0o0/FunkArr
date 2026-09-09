namespace FunkArr.Messages.Scoring;

public sealed record MatchingConfig(
    string RuleSetId,
    float DefaultConfidence,
    MatchingRule[] Rules);
