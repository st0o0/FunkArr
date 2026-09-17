using FunkArr.Messages.Scoring;

namespace FunkArr.Messages.RuleSet;

public sealed record QueryRuleSetDetail(string RuleSetId);

public abstract record RuleSetDetailResponse;

public sealed record RuleSetDetailResult(
    string RuleSetId,
    RuleSetDetailResult.RuleSetIdentity Identity,
    RuleSetDetailResult.RuleSetSource Source,
    float DefaultConfidence,
    RuleSetDetailRule[] Rules) : RuleSetDetailResponse
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

public sealed record RuleSetDetailFailed(Exception Cause) : RuleSetDetailResponse;

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
