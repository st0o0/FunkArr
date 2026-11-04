using FunkArr.Api.Models.RuleSets;
using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Models.Scoring;

public enum MatchMethod
{
    RegexExtracted,
    TitleMatch,
    AirdateMatch,
    YearMatch
}

public enum RuleOutcome
{
    Matched,
    FilterFailed,
    IdentificationFailed
}

public enum SearchSource
{
    Sonarr,
    Radarr,
    Prowlarr,
    Feed = 4
}

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
    EnrichmentTraceOutput? EnrichmentTrace = null,
    MediathekVariant? ExcludedVariant = null);

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

public enum FilterOperator
{
    All,
    Any,
    Not
}

public sealed record FilterGroupTrace(
    FilterOperator Operator,
    bool Passed,
    FilterNodeTrace[] Nodes);

public sealed record FilterNodeTrace(
    FilterField? Field,
    FilterOp? Op,
    string? ExpectedValue,
    string? ActualValue,
    bool Passed,
    bool Skipped,
    FilterGroupTrace? Group);

public sealed record IdentificationTrace(
    IdentificationStrategy? Strategy,
    bool Attempted,
    string? Detail);

public sealed record TracedIdentification(
    string? Season,
    string? Episode,
    string? Title);
