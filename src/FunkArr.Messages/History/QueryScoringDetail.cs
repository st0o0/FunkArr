using FunkArr.Messages;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Messages.History;

public sealed record QueryScoringDetail(
    string RuleSetId,
    Guid RequestId) : IWithRuleSetId;

public abstract record ScoringDetailResponse;

public sealed record ScoringDetailResult(
    Guid RequestId,
    SearchSource Source,
    string Query,
    DateTimeOffset Timestamp,
    ItemTrace[] ItemTraces) : ScoringDetailResponse;

public sealed record ScoringDetailFailed(Exception Cause) : ScoringDetailResponse;
