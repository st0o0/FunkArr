using FunkArr.Messages.Scoring.History;

namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record ScoringRecorded(
    Guid RequestId,
    string Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    ItemTrace[] ItemTraces);
