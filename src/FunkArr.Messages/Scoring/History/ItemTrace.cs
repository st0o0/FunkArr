namespace FunkArr.Messages.Scoring.History;

public sealed record ItemTrace(
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
    TracedIdentification? Identification,
    RuleTrace[] RuleTraces);
