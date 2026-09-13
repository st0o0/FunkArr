namespace FunkArr.Api.Models;

public sealed record RuleSetListResponse(
    string? CommunityVersion,
    RuleSetListEntry[] Rulesets);
