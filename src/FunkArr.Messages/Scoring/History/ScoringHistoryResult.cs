namespace FunkArr.Messages.Scoring.History;

public sealed record ScoringHistoryResult(
    string RuleSetId,
    int TotalCount,
    ScoringSnapshotSummary[] Snapshots);
