namespace FunkArr.Api.Models;

public sealed record RuleSetDetail(
    string RuleSetId,
    RuleSetDetail.RuleSetIdentity Identity,
    RuleSetDetail.RuleSetSource Source,
    float DefaultConfidence,
    RuleSetDetailRule[] Rules)
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
    string Strategy,
    string? FilterSummary,
    string? SeasonPattern,
    string? EpisodePattern,
    string? MatchMode,
    string[]? TitleParts);
