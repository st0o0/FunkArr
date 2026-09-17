namespace FunkArr.Messages.Scoring.History;

public sealed record QueryScoringHistory(
    string RuleSetId,
    int Offset,
    int Limit) : IWithRuleSetId;

public abstract record ScoringHistoryResponse;

public sealed record ScoringHistoryResult(
    string RuleSetId,
    int TotalCount,
    ScoringSnapshotSummary[] Snapshots) : ScoringHistoryResponse;
