using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.ScoringHistory;

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
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };

    public static SearchSource ToDomain(this PersistedSearchSource source) =>
        source switch
        {
            PersistedSearchSource.Sonarr => SearchSource.Sonarr,
            PersistedSearchSource.Radarr => SearchSource.Radarr,
            PersistedSearchSource.Prowlarr => SearchSource.Prowlarr,
            PersistedSearchSource.Test => SearchSource.Test,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };

    public static PersistedItemTrace ToPersistence(this ItemTrace trace) =>
        new(trace.CandidateTitle, trace.CandidateTopic, trace.CandidateChannel,
            trace.CandidateDuration, trace.CandidateQuality, trace.CandidateDescription,
            trace.CandidateTimestamp, trace.Matched, trace.Score, trace.MatchedRuleId,
            trace.Identification?.ToPersistence(),
            trace.RuleTraces.Select(r => r.ToPersistence()).ToArray(),
            trace.EnrichmentTrace?.ToPersistence());

    public static ItemTrace ToDomain(this PersistedItemTrace trace) =>
        new(trace.CandidateTitle, trace.CandidateTopic, trace.CandidateChannel,
            trace.CandidateDuration, trace.CandidateQuality, trace.CandidateDescription,
            trace.CandidateTimestamp, trace.Matched, trace.Score, trace.MatchedRuleId,
            trace.Identification?.ToDomain(),
            trace.RuleTraces.Select(r => r.ToDomain()).ToArray(),
            trace.EnrichmentTrace?.ToDomain());

    public static PersistedRuleTrace ToPersistence(this RuleTrace trace) =>
        new(trace.RuleId, trace.Priority, (PersistedRuleOutcome)(int)trace.Outcome,
            trace.FilterTrace?.ToPersistence(), trace.IdentificationTrace?.ToPersistence());

    public static RuleTrace ToDomain(this PersistedRuleTrace trace) =>
        new(trace.RuleId, trace.Priority, (RuleOutcome)(int)trace.Outcome,
            trace.FilterTrace?.ToDomain(), trace.IdentificationTrace?.ToDomain());

    public static PersistedFilterGroupTrace ToPersistence(this FilterGroupTrace trace) =>
        new(trace.Operator.ToString(), trace.Passed, trace.Nodes.Select(n => n.ToPersistence()).ToArray());

    public static FilterGroupTrace ToDomain(this PersistedFilterGroupTrace trace) =>
        new(Enum.Parse<FilterGroupOp>(trace.Operator), trace.Passed, trace.Nodes.Select(n => n.ToDomain()).ToArray());

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
        new(trace.Strategy?.ToString(), trace.Attempted, trace.Detail?.ToString());

    public static IdentificationTrace ToDomain(this PersistedIdentificationTrace trace) =>
        new(trace.Strategy is not null ? Enum.Parse<IdentificationStrategy>(trace.Strategy) : null,
            trace.Attempted,
            trace.Detail is not null ? Enum.Parse<IdentificationFailureReason>(trace.Detail) : null);

    public static PersistedEnrichmentTrace ToPersistence(this EnrichmentTrace trace) =>
        new((PersistedMatchMethod)(int)trace.Method, trace.Confidence, trace.Enriched,
            trace.ResolvedSeason, trace.ResolvedEpisode, trace.ResolvedTitle,
            trace.ResolvedYear, trace.DaysDiff, trace.Detail);

    public static EnrichmentTrace ToDomain(this PersistedEnrichmentTrace trace) =>
        new((MatchMethod)(int)trace.Method, trace.Confidence, trace.Enriched,
            trace.ResolvedSeason, trace.ResolvedEpisode, trace.ResolvedTitle,
            trace.ResolvedYear, trace.DaysDiff, trace.Detail);
}
