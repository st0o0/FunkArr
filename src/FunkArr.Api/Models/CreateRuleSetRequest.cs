using System.ComponentModel.DataAnnotations;

namespace FunkArr.Api.Models;

public sealed record CreateRuleSetRequest(
    [property: Required, RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "ruleSetId must be kebab-case (lowercase letters, numbers, hyphens)")]
    string RuleSetId,
    [property: Required] string Topic,
    MediaInput Media,
    [property: Required, MinLength(1)] RuleInput[] Rules,
    string[]? Aliases = null,
    [property: Range(0.0, 1.0)] float? Confidence = null,
    bool? Standalone = null,
    string[]? Disable = null,
    EnrichmentConfigInput? Enrichment = null) : IRuleSetBody;
