using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Shared;

namespace FunkArr.Messages.RuleSet;

public sealed record RegisterRuleSet(
    string RuleSetId,
    string Topic,
    string[] Aliases,
    ExternalIds? Ids = null,
    string? MediaName = null,
    MediaType? MediaType = null,
    EnrichmentConfig? Enrichment = null);
