namespace FunkArr.Messages.RuleSet;

public sealed record QueryRuleSetDetail(string RuleSetId);

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

public sealed record RuleSetDetailResult(
    string RuleSetId,
    RuleSetDetailResult.RuleSetIdentity Identity,
    RuleSetDetailResult.RuleSetSource Source,
    float DefaultConfidence,
    RuleSetDetailRule[] Rules) : IRuleSetResponse
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
