namespace FunkArr.Api.Models;

public sealed record VersionResponse(
    string AppVersion,
    string? CommunityRulesetVersion);
