namespace FunkArr.Messages.Scoring.History;

public sealed record ItemTrace(
    ScoreCandidate Candidate,
    bool Matched,
    double Score,
    string? MatchedRuleId,
    TracedIdentification? Identification,
    RuleTrace[] RuleTraces,
    EnrichmentTrace? EnrichmentTrace = null);
