namespace FunkArr.Messages.History;

public sealed record QueryScoringStats(string RuleSetId) : IWithRuleSetId;

public sealed record ScoringStatsResult(
    DateTimeOffset? LastRun,
    double? MatchRate,
    double? EnrichmentRate = null,
    int TotalRuns = 0);
