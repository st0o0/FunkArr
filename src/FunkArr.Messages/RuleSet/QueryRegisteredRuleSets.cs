namespace FunkArr.Messages.RuleSet;

public sealed record QueryRegisteredRuleSets;

public sealed record RegisteredRuleSetEntry(
    string RuleSetId,
    string Topic,
    string[] Aliases,
    int? TvdbId,
    string? ImdbId,
    int? TmdbId,
    string? MediaName);

public sealed record RegisteredRuleSetsResult(RegisteredRuleSetEntry[] Entries) : IRuleSetResponse;
