namespace FunkArr.Api.Models;

public sealed record ScoringHistory(
    string RuleSetId,
    int TotalCount,
    ScoringSnapshotSummary[] Snapshots);

public sealed record ScoringSnapshotSummary(
    Guid RequestId,
    string Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount);
