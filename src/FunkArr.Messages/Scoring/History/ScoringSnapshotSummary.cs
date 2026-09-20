using FunkArr.Messages;

namespace FunkArr.Messages.Scoring.History;

public sealed record ScoringSnapshotSummary(
    Guid RequestId,
    SearchSource Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount);
