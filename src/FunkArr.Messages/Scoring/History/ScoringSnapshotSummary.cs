namespace FunkArr.Messages.Scoring.History;

public sealed record ScoringSnapshotSummary(
    Guid RequestId,
    string Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount);
