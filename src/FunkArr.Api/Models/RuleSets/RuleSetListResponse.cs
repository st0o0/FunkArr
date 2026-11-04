using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Models.RuleSets;

public enum SourceType
{
    Unknown = 0,
    Community = 1,
    Local = 2,
    Merged = 3
}

public sealed record RuleSetListResponse(
    string? CommunityVersion,
    RuleSetListEntry[] Rulesets);

public sealed record RuleSetListEntry(
    string RuleSetId,
    string Topic,
    string[] Aliases,
    int? TvdbId,
    string? ImdbId,
    int? TmdbId,
    string? MediaName,
    MediaType? MediaType,
    int RuleCount,
    SourceType SourceType,
    DateTimeOffset? LastScoringRun,
    double? MatchRate,
    double? EnrichmentRate = null);
