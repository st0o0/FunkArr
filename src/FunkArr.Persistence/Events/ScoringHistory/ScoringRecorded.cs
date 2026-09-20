using FunkArr.Messages;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record ScoringRecorded(
    Guid RequestId,
    SearchSource Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    ItemTrace[] ItemTraces);
