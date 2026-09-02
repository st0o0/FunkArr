namespace FunkArr.Messages.RuleSet;

public sealed record QueryRegisteredRuleSets;

public sealed record RegisteredRuleSetEntry(
    string RuleSetId,
    string Topic,
    string[] Aliases,
    int? TvdbId,
    string? ImdbId,
    int? TmdbId);

public sealed record RegisteredRuleSetsResult(RegisteredRuleSetEntry[] Entries);
