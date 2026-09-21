using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Messages.History;

public sealed record RecordHistory(
    Guid RequestId,
    string RuleSetId,
    ScoringOrigin Origin,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    int EnrichedCount,
    ItemTrace[] ItemTraces) : IWithRuleSetId;
