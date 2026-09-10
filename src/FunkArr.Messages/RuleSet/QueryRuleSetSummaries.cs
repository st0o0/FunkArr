namespace FunkArr.Messages.RuleSet;

public sealed record QueryRuleSetSummaries;

public sealed record RuleSetSummaryEntry(
    string RuleSetId,
    int RuleCount,
    string SourceType);

public sealed record RuleSetSummaryResult(RuleSetSummaryEntry[] Entries) : IRuleSetResponse;
