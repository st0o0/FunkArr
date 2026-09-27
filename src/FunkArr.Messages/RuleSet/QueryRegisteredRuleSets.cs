using FunkArr.Messages.Shared;

namespace FunkArr.Messages.RuleSet;

public sealed record QueryRegisteredRuleSets;

public sealed record RegisteredRuleSetEntry(
    string RuleSetId,
    string Topic,
    string[] Aliases,
    ExternalIds Ids,
    string? MediaName,
    MediaType? MediaType);

public sealed record RegisteredRuleSetsResult(RegisteredRuleSetEntry[] Entries);
