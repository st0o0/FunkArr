using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedItemTrace(
    PersistedScoreCandidate Candidate,
    bool Matched,
    double Score,
    string? MatchedRuleId,
    PersistedTracedIdentification? Identification,
    PersistedRuleTrace[] RuleTraces,
    PersistedEnrichmentTrace? EnrichmentTrace = null);
