namespace FunkArr.Messages.Scoring.History;

public sealed record QueryScoringStats(string RuleSetId) : IWithRuleSetId;

public sealed record ScoringStatsResult(DateTimeOffset? LastRun, double? MatchRate);
