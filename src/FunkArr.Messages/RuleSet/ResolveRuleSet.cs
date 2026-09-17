namespace FunkArr.Messages.RuleSet;

public sealed record ResolveRuleSet(
    string? TopicOrAlias,
    int? TvdbId = null,
    string? ImdbId = null,
    int? TmdbId = null);

public abstract record ResolveRuleSetResponse;

public sealed record RuleSetResolved(string RuleSetId, string Topic, string? MediaName = null) : ResolveRuleSetResponse;

public sealed record RuleSetFailed(Exception Cause) : ResolveRuleSetResponse;
