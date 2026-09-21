using FunkArr.Messages;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record HistoryRecorded(
    Guid RequestId,
    SearchSource Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    int EnrichedCount,
    ItemTrace[] ItemTraces);
