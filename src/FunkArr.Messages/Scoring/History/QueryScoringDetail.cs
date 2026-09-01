namespace FunkArr.Messages.Scoring.History;

public sealed record QueryScoringDetail(
    string RuleSetId,
    Guid RequestId) : IWithRuleSetId;
