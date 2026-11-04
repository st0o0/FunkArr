using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.ScoringHistory;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.History;

internal static class PersistenceMapping
{
    public static PersistedSearchSource ToPersistence(this SearchSource source) =>
        source switch
        {
            SearchSource.Sonarr => PersistedSearchSource.Sonarr,
            SearchSource.Radarr => PersistedSearchSource.Radarr,
            SearchSource.Prowlarr => PersistedSearchSource.Prowlarr,
            SearchSource.Feed => PersistedSearchSource.Feed,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

    public static SearchSource ToDomain(this PersistedSearchSource source) =>
        source switch
        {
            PersistedSearchSource.Sonarr => SearchSource.Sonarr,
            PersistedSearchSource.Radarr => SearchSource.Radarr,
            PersistedSearchSource.Prowlarr => SearchSource.Prowlarr,
            PersistedSearchSource.Feed => SearchSource.Feed,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

    public static PersistedItemTrace ToPersistence(this ItemTrace trace) =>
        new(new PersistedScoreCandidate(
                trace.Candidate.Title, trace.Candidate.Topic, trace.Candidate.Channel,
                trace.Candidate.Duration, trace.Candidate.Quality, trace.Candidate.Description,
                trace.Candidate.Timestamp),
            trace.Matched, trace.Score, trace.MatchedRuleId,
            trace.Identification?.ToPersistence(),
            [.. trace.RuleTraces.Select(r => r.ToPersistence())],
            trace.EnrichmentTrace?.ToPersistence(),
            trace.ExcludedVariant is { } excluded ? excluded.ToPersistence() : null);

    public static ItemTrace ToDomain(this PersistedItemTrace trace) =>
        new(new ScoreCandidate(
                trace.Candidate.Title, trace.Candidate.Topic, trace.Candidate.Channel,
                trace.Candidate.Duration, trace.Candidate.Quality, trace.Candidate.Description,
                trace.Candidate.Timestamp),
            trace.Matched, trace.Score, trace.MatchedRuleId,
            trace.Identification?.ToDomain(),
            [.. trace.RuleTraces.Select(r => r.ToDomain())],
            trace.EnrichmentTrace?.ToDomain(),
            trace.ExcludedVariant is { } excluded ? excluded.ToDomain() : null);

    public static PersistedRuleTrace ToPersistence(this RuleTrace trace) =>
        new(trace.RuleId, trace.Priority, trace.Outcome.ToPersistence(),
            trace.FilterTrace?.ToPersistence(), trace.IdentificationTrace?.ToPersistence());

    public static RuleTrace ToDomain(this PersistedRuleTrace trace) =>
        new(trace.RuleId, trace.Priority, trace.Outcome.ToDomain(),
            trace.FilterTrace?.ToDomain(), trace.IdentificationTrace?.ToDomain());

    public static PersistedFilterGroupTrace ToPersistence(this FilterGroupTrace trace) =>
        new(trace.Operator.ToPersistence(), trace.Passed, [.. trace.Nodes.Select(n => n.ToPersistence())]);

    public static FilterGroupTrace ToDomain(this PersistedFilterGroupTrace trace) =>
        new(trace.Operator.ToDomain(), trace.Passed, [.. trace.Nodes.Select(n => n.ToDomain())]);

    public static PersistedFilterNodeTrace ToPersistence(this FilterNodeTrace trace) =>
        new(trace.Field, trace.Op, trace.ExpectedValue, trace.ActualValue,
            trace.Passed, trace.Skipped, trace.Group?.ToPersistence());

    public static FilterNodeTrace ToDomain(this PersistedFilterNodeTrace trace) =>
        new(trace.Field, trace.Op, trace.ExpectedValue, trace.ActualValue,
            trace.Passed, trace.Skipped, trace.Group?.ToDomain());

    public static PersistedTracedIdentification ToPersistence(this TracedIdentification trace) =>
        new(trace.Season, trace.Episode, trace.Title);

    public static TracedIdentification ToDomain(this PersistedTracedIdentification trace) =>
        new(trace.Season, trace.Episode, trace.Title);

    public static PersistedIdentificationTrace ToPersistence(this IdentificationTrace trace) =>
        new(trace.Strategy is not null ? trace.Strategy.Value.ToPersistence() : null,
            trace.Attempted,
            trace.Detail is not null ? trace.Detail.Value.ToPersistence() : null);

    public static IdentificationTrace ToDomain(this PersistedIdentificationTrace trace) =>
        new(trace.Strategy is not null ? trace.Strategy.Value.ToDomain() : null,
            trace.Attempted,
            trace.Detail is not null ? trace.Detail.Value.ToDomain() : null);

    public static PersistedEnrichmentTrace ToPersistence(this EnrichmentTrace trace) =>
        new(trace.Method.ToPersistence(), trace.Confidence, trace.Enriched,
            trace.ResolvedSeason, trace.ResolvedEpisode, trace.ResolvedTitle,
            trace.ResolvedYear, trace.DaysDiff, trace.Detail);

    public static EnrichmentTrace ToDomain(this PersistedEnrichmentTrace trace) =>
        new(trace.Method.ToDomain(), trace.Confidence, trace.Enriched,
            trace.ResolvedSeason, trace.ResolvedEpisode, trace.ResolvedTitle,
            trace.ResolvedYear, trace.DaysDiff, trace.Detail);

    public static PersistedMediathekVariant ToPersistence(this MediathekVariant value) =>
        value switch
        {
            MediathekVariant.Standard => PersistedMediathekVariant.Standard,
            MediathekVariant.AudioDescription => PersistedMediathekVariant.AudioDescription,
            MediathekVariant.SignLanguage => PersistedMediathekVariant.SignLanguage,
            MediathekVariant.PlainLanguage => PersistedMediathekVariant.PlainLanguage,
            MediathekVariant.OriginalVersion => PersistedMediathekVariant.OriginalVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MediathekVariant ToDomain(this PersistedMediathekVariant value) =>
        value switch
        {
            PersistedMediathekVariant.Standard => MediathekVariant.Standard,
            PersistedMediathekVariant.AudioDescription => MediathekVariant.AudioDescription,
            PersistedMediathekVariant.SignLanguage => MediathekVariant.SignLanguage,
            PersistedMediathekVariant.PlainLanguage => MediathekVariant.PlainLanguage,
            PersistedMediathekVariant.OriginalVersion => MediathekVariant.OriginalVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static PersistedRuleOutcome ToPersistence(this RuleOutcome value) =>
        value switch
        {
            RuleOutcome.Matched => PersistedRuleOutcome.Matched,
            RuleOutcome.FilterFailed => PersistedRuleOutcome.FilterFailed,
            RuleOutcome.IdentificationFailed => PersistedRuleOutcome.IdentificationFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static RuleOutcome ToDomain(this PersistedRuleOutcome value) =>
        value switch
        {
            PersistedRuleOutcome.Matched => RuleOutcome.Matched,
            PersistedRuleOutcome.FilterFailed => RuleOutcome.FilterFailed,
            PersistedRuleOutcome.IdentificationFailed => RuleOutcome.IdentificationFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static PersistedFilterGroupOp ToPersistence(this FilterGroupOp value) =>
        value switch
        {
            FilterGroupOp.All => PersistedFilterGroupOp.All,
            FilterGroupOp.Any => PersistedFilterGroupOp.Any,
            FilterGroupOp.Not => PersistedFilterGroupOp.Not,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static FilterGroupOp ToDomain(this PersistedFilterGroupOp value) =>
        value switch
        {
            PersistedFilterGroupOp.All => FilterGroupOp.All,
            PersistedFilterGroupOp.Any => FilterGroupOp.Any,
            PersistedFilterGroupOp.Not => FilterGroupOp.Not,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static PersistedIdentificationStrategy ToPersistence(this IdentificationStrategy value) =>
        value switch
        {
            IdentificationStrategy.SeasonAndEpisodeNumber => PersistedIdentificationStrategy.SeasonAndEpisodeNumber,
            IdentificationStrategy.AbsoluteEpisodeNumber => PersistedIdentificationStrategy.AbsoluteEpisodeNumber,
            IdentificationStrategy.TitleExact => PersistedIdentificationStrategy.TitleExact,
            IdentificationStrategy.TitleIncludes => PersistedIdentificationStrategy.TitleIncludes,
            IdentificationStrategy.AirdateExtraction => PersistedIdentificationStrategy.AirdateExtraction,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static IdentificationStrategy ToDomain(this PersistedIdentificationStrategy value) =>
        value switch
        {
            PersistedIdentificationStrategy.SeasonAndEpisodeNumber => IdentificationStrategy.SeasonAndEpisodeNumber,
            PersistedIdentificationStrategy.AbsoluteEpisodeNumber => IdentificationStrategy.AbsoluteEpisodeNumber,
            PersistedIdentificationStrategy.TitleExact => IdentificationStrategy.TitleExact,
            PersistedIdentificationStrategy.TitleIncludes => IdentificationStrategy.TitleIncludes,
            PersistedIdentificationStrategy.AirdateExtraction => IdentificationStrategy.AirdateExtraction,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static PersistedIdentificationFailureReason ToPersistence(this IdentificationFailureReason value) =>
        value switch
        {
            IdentificationFailureReason.UnknownStrategy => PersistedIdentificationFailureReason.UnknownStrategy,
            IdentificationFailureReason.SeasonPatternNotMatched => PersistedIdentificationFailureReason.SeasonPatternNotMatched,
            IdentificationFailureReason.NoEpisodePatternConfigured => PersistedIdentificationFailureReason.NoEpisodePatternConfigured,
            IdentificationFailureReason.EpisodePatternNotMatched => PersistedIdentificationFailureReason.EpisodePatternNotMatched,
            IdentificationFailureReason.NoTitlePartsConfigured => PersistedIdentificationFailureReason.NoTitlePartsConfigured,
            IdentificationFailureReason.TitlePartRegexNotMatched => PersistedIdentificationFailureReason.TitlePartRegexNotMatched,
            IdentificationFailureReason.TitleDoesNotMatch => PersistedIdentificationFailureReason.TitleDoesNotMatch,
            IdentificationFailureReason.NoDateFoundInTitle => PersistedIdentificationFailureReason.NoDateFoundInTitle,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static IdentificationFailureReason ToDomain(this PersistedIdentificationFailureReason value) =>
        value switch
        {
            PersistedIdentificationFailureReason.UnknownStrategy => IdentificationFailureReason.UnknownStrategy,
            PersistedIdentificationFailureReason.SeasonPatternNotMatched => IdentificationFailureReason.SeasonPatternNotMatched,
            PersistedIdentificationFailureReason.NoEpisodePatternConfigured => IdentificationFailureReason.NoEpisodePatternConfigured,
            PersistedIdentificationFailureReason.EpisodePatternNotMatched => IdentificationFailureReason.EpisodePatternNotMatched,
            PersistedIdentificationFailureReason.NoTitlePartsConfigured => IdentificationFailureReason.NoTitlePartsConfigured,
            PersistedIdentificationFailureReason.TitlePartRegexNotMatched => IdentificationFailureReason.TitlePartRegexNotMatched,
            PersistedIdentificationFailureReason.TitleDoesNotMatch => IdentificationFailureReason.TitleDoesNotMatch,
            PersistedIdentificationFailureReason.NoDateFoundInTitle => IdentificationFailureReason.NoDateFoundInTitle,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static PersistedMatchMethod ToPersistence(this MatchMethod value) =>
        value switch
        {
            MatchMethod.RegexExtracted => PersistedMatchMethod.RegexExtracted,
            MatchMethod.TitleMatch => PersistedMatchMethod.TitleMatch,
            MatchMethod.AirdateMatch => PersistedMatchMethod.AirdateMatch,
            MatchMethod.YearMatch => PersistedMatchMethod.YearMatch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MatchMethod ToDomain(this PersistedMatchMethod value) =>
        value switch
        {
            PersistedMatchMethod.RegexExtracted => MatchMethod.RegexExtracted,
            PersistedMatchMethod.TitleMatch => MatchMethod.TitleMatch,
            PersistedMatchMethod.AirdateMatch => MatchMethod.AirdateMatch,
            PersistedMatchMethod.YearMatch => MatchMethod.YearMatch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
