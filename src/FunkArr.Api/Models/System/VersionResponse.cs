namespace FunkArr.Api.Models.System;

public sealed record VersionResponse(
    string AppVersion,
    string? CommunityRulesetVersion);
