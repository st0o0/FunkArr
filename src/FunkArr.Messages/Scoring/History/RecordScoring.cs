namespace FunkArr.Messages.Scoring.History;

public sealed record RecordScoring(
    Guid RequestId,
    string RuleSetId,
    ScoringOrigin Origin,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    ItemTrace[] ItemTraces) : IWithRuleSetId;
