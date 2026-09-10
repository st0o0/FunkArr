namespace FunkArr.Messages.Scoring.History;

public sealed record QueryScoringStats(string RuleSetId);

public sealed record ScoringStatsResult(DateTimeOffset? LastRun, double? MatchRate);
