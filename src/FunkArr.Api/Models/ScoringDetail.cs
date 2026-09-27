namespace FunkArr.Api.Models;

public sealed record ScoringDetail(
    Guid RequestId,
    SearchSource Source,
    string Query,
    DateTimeOffset Timestamp,
    ItemTrace[] ItemTraces);

public sealed record ItemTrace(
    ScoreCandidate Candidate,
    bool Matched,
    double Score,
    string? MatchedRuleId,
    TracedIdentification? Identification,
    RuleTrace[] RuleTraces,
    EnrichmentTraceOutput? EnrichmentTrace = null);

public sealed record ScoreCandidate(
    string Title,
    string Topic,
    string Channel,
    int Duration,
    int Quality,
    string? Description,
    long Timestamp);

public sealed record RuleTrace(
    string RuleId,
    int Priority,
    RuleOutcome Outcome,
    FilterGroupTrace? FilterTrace,
    IdentificationTrace? IdentificationTrace);

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
