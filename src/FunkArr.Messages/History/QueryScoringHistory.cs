namespace FunkArr.Messages.History;

public sealed record QueryScoringHistory(
    string HistoryId,
    int Offset,
    int Limit) : IWithHistoryId;

public abstract record ScoringHistoryResponse;

public sealed record ScoringHistoryResult(
    string RuleSetId,
    int TotalCount,
    ScoringSnapshotSummary[] Snapshots) : ScoringHistoryResponse;
