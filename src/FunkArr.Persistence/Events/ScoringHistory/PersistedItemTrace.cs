namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedItemTrace(
    string CandidateTitle,
    string CandidateTopic,
    string CandidateChannel,
    int CandidateDuration,
    int CandidateQuality,
    string? CandidateDescription,
    long CandidateTimestamp,
    bool Matched,
    double Score,
    string? MatchedRuleId,
    PersistedTracedIdentification? Identification,
    PersistedRuleTrace[] RuleTraces,
    PersistedEnrichmentTrace? EnrichmentTrace = null);
