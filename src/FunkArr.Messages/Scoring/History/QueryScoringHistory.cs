namespace FunkArr.Messages.Scoring.History;

public sealed record QueryScoringHistory(
    string RuleSetId,
    int Offset,
    int Limit) : IWithRuleSetId;
