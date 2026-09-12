namespace FunkArr.Api.Models;

public sealed record RuleSetListEntry(
    string RuleSetId,
    string Topic,
    string[] Aliases,
    int? TvdbId,
    string? ImdbId,
    int? TmdbId,
    string? MediaName,
    string? MediaType,
    int RuleCount,
    string SourceType,
    string? LastScoringRun,
    double? MatchRate);
