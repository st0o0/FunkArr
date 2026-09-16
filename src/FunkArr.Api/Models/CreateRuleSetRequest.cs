namespace FunkArr.Api.Models;

public sealed record CreateRuleSetRequest(
    string RuleSetId,
    string Topic,
    MediaInput Media,
    RuleInput[] Rules,
    string[]? Aliases = null,
    float? Confidence = null,
    bool? Standalone = null,
    string[]? Disable = null);
