using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Shared;

namespace FunkArr.Messages.RuleSet;

public sealed record ResolveRuleSet(
    string? TopicOrAlias,
    ExternalIds? Ids = null);

public abstract record ResolveRuleSetResponse;

public sealed record RuleSetResolved(
    string RuleSetId,
    string Topic,
    string? MediaName = null,
    EnrichmentConfig? Enrichment = null) : ResolveRuleSetResponse;

public sealed record RuleSetFailed(Exception Cause) : ResolveRuleSetResponse;
