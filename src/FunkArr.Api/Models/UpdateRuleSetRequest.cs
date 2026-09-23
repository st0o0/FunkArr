using System.ComponentModel.DataAnnotations;

namespace FunkArr.Api.Models;

public sealed record UpdateRuleSetRequest(
    [property: Required] string Topic,
    MediaInput Media,
    [property: Required, MinLength(1)] RuleInput[] Rules,
    string[]? Aliases = null,
    [property: Range(0.0, 1.0)] float? Confidence = null,
    bool? Standalone = null,
    string[]? Disable = null,
    EnrichmentConfigInput? Enrichment = null) : IRuleSetBody;
