namespace FunkArr.Messages.Scoring.History;

public sealed record RecordScoringResult(
    Guid RequestId,
    string RuleSetId,
    ScoringOrigin Origin,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    ItemTrace[] ItemTraces) : IWithRuleSetId;
