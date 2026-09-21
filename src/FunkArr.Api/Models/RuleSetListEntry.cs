namespace FunkArr.Api.Models;

public enum SourceType
{
    Community,
    Local,
    Merged,
    Unknown,
}

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
