namespace FunkArr.Messages.RuleSet;

public sealed record RegisterRuleSet(
    string RuleSetId,
    string Topic,
    string[] Aliases,
    int? TvdbId = null,
    string? ImdbId = null,
    int? TmdbId = null);
