namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record ScoringHistoryRecorded(
    Guid RequestId,
    PersistedSearchSource Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    int EnrichedCount,
    PersistedItemTrace[] ItemTraces);
