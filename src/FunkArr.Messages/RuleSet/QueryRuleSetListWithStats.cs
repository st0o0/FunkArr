namespace FunkArr.Messages.RuleSet;

public sealed record QueryRuleSetListWithStats;

public sealed record RuleSetListWithStatsEntry(
    string RuleSetId,
    int RuleCount,
    string SourceType,
    DateTimeOffset? LastRun,
    double? MatchRate);

public sealed record RuleSetListWithStatsResult(
    RuleSetListWithStatsEntry[] Entries);
