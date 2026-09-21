using FunkArr.Messages.Scoring;

namespace FunkArr.Api.Models;

public sealed record RuleSetDetail(
    string RuleSetId,
    RuleSetDetail.RuleSetIdentity Identity,
    RuleSetDetail.RuleSetSource Source,
    float DefaultConfidence,
    RuleSetDetailRule[] Rules,
    EnrichmentConfigOutput Enrichment)
{
    public sealed record RuleSetIdentity(
        string Topic,
        string[] Aliases,
        int? TvdbId,
        string? ImdbId,
        int? TmdbId);

    public sealed record RuleSetSource(
        string? CommunityPath,
        string? LocalPath,
        DateTime? CommunityModified,
        DateTime? LocalModified);
}

public sealed record RuleSetDetailRule(
    string Id,
    int Priority,
    float? Confidence,
    IdentificationStrategy Strategy,
    string? SeasonRegex,
    string? EpisodeRegex,
    int? CaptureGroup,
    FilterGroupOutput? Filters,
    TitleRuleOutput[]? TitleRules);
