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
            SearchSource.Test => PersistedSearchSource.Test,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

    public static SearchSource ToDomain(this PersistedSearchSource source) =>
        source switch
        {
            PersistedSearchSource.Sonarr => SearchSource.Sonarr,
            PersistedSearchSource.Radarr => SearchSource.Radarr,
            PersistedSearchSource.Prowlarr => SearchSource.Prowlarr,
            PersistedSearchSource.Test => SearchSource.Test,
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
            trace.EnrichmentTrace?.ToPersistence());

    public static ItemTrace ToDomain(this PersistedItemTrace trace) =>
        new(new ScoreCandidate(
                trace.Candidate.Title, trace.Candidate.Topic, trace.Candidate.Channel,
                trace.Candidate.Duration, trace.Candidate.Quality, trace.Candidate.Description,
                trace.Candidate.Timestamp),
            trace.Matched, trace.Score, trace.MatchedRuleId,
            trace.Identification?.ToDomain(),
            [.. trace.RuleTraces.Select(r => r.ToDomain())],
            trace.EnrichmentTrace?.ToDomain());

    public static PersistedRuleTrace ToPersistence(this RuleTrace trace) =>
        new(trace.RuleId, trace.Priority, (PersistedRuleOutcome)(int)trace.Outcome,
            trace.FilterTrace?.ToPersistence(), trace.IdentificationTrace?.ToPersistence());

    public static RuleTrace ToDomain(this PersistedRuleTrace trace) =>
        new(trace.RuleId, trace.Priority, (RuleOutcome)(int)trace.Outcome,
            trace.FilterTrace?.ToDomain(), trace.IdentificationTrace?.ToDomain());

    public static PersistedFilterGroupTrace ToPersistence(this FilterGroupTrace trace) =>
        new((PersistedFilterGroupOp)(int)trace.Operator, trace.Passed, [.. trace.Nodes.Select(n => n.ToPersistence())]);

    public static FilterGroupTrace ToDomain(this PersistedFilterGroupTrace trace) =>
        new((FilterGroupOp)(int)trace.Operator, trace.Passed, [.. trace.Nodes.Select(n => n.ToDomain())]);

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
        new(trace.Strategy is not null ? (PersistedIdentificationStrategy)(int)trace.Strategy.Value : null,
            trace.Attempted,
            trace.Detail is not null ? (PersistedIdentificationFailureReason)(int)trace.Detail.Value : null);

    public static IdentificationTrace ToDomain(this PersistedIdentificationTrace trace) =>
        new(trace.Strategy is not null ? (IdentificationStrategy)(int)trace.Strategy.Value : null,
            trace.Attempted,
            trace.Detail is not null ? (IdentificationFailureReason)(int)trace.Detail.Value : null);

    public static PersistedEnrichmentTrace ToPersistence(this EnrichmentTrace trace) =>
        new((PersistedMatchMethod)(int)trace.Method, trace.Confidence, trace.Enriched,
            trace.ResolvedSeason, trace.ResolvedEpisode, trace.ResolvedTitle,
            trace.ResolvedYear, trace.DaysDiff, trace.Detail);

    public static EnrichmentTrace ToDomain(this PersistedEnrichmentTrace trace) =>
        new((MatchMethod)(int)trace.Method, trace.Confidence, trace.Enriched,
            trace.ResolvedSeason, trace.ResolvedEpisode, trace.ResolvedTitle,
            trace.ResolvedYear, trace.DaysDiff, trace.Detail);
}
