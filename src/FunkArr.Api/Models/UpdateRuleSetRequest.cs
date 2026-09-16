namespace FunkArr.Api.Models;

public sealed record UpdateRuleSetRequest(
    string Topic,
    MediaInput Media,
    RuleInput[] Rules,
    string[]? Aliases = null,
    float? Confidence = null,
    bool? Standalone = null,
    string[]? Disable = null);
