namespace FunkArr.Messages.Scoring.History;

public sealed record QueryScoringDetail(
    string RuleSetId,
    Guid RequestId) : IWithRuleSetId;

public abstract record ScoringDetailResponse;

public sealed record ScoringDetailResult(
    Guid RequestId,
    string Source,
    string Query,
    DateTimeOffset Timestamp,
    ItemTrace[] ItemTraces) : ScoringDetailResponse;

public sealed record ScoringDetailFailed(Exception Cause) : ScoringDetailResponse;
