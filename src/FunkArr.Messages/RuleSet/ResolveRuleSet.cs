namespace FunkArr.Messages.RuleSet;

public sealed record ResolveRuleSet(
    string? TopicOrAlias,
    int? TvdbId = null,
    string? ImdbId = null,
    int? TmdbId = null);
