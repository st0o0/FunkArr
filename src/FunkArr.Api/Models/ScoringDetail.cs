namespace FunkArr.Api.Models;

public sealed record ScoringDetail(
    Guid RequestId,
    string Source,
    string Query,
    DateTimeOffset Timestamp,
    ItemTrace[] ItemTraces);

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

public sealed record RuleTrace(
    string RuleId,
    int Priority,
    RuleOutcome Outcome,
    FilterGroupTrace? FilterTrace,
    IdentificationTrace? IdentificationTrace);

public enum RuleOutcome
{
    Matched,
    FilterFailed,
    IdentificationFailed
}

public sealed record FilterGroupTrace(
    string Operator,
    bool Passed,
    FilterNodeTrace[] Nodes);

public sealed record FilterNodeTrace(
    string? Field,
    string? Op,
    string? ExpectedValue,
    string? ActualValue,
    bool Passed,
    bool Skipped,
    FilterGroupTrace? Group);

public sealed record IdentificationTrace(
    string? Strategy,
    bool Attempted,
    string? Detail);

public sealed record TracedIdentification(
    string? Season,
    string? Episode,
    string? Title);
