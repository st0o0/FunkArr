namespace FunkArr.Api.Models;

public sealed record TestScoreRequest(
    float DefaultConfidence,
    RuleInput[] Rules,
    TestCandidate[] Candidates,
    EnrichmentConfigInput? Enrichment = null,
    int? TvdbId = null,
    int? TmdbId = null,
    string? ImdbId = null,
    string? MediaType = null);

public sealed record TestCandidate(
    string Title = "",
    string Topic = "",
    string Channel = "",
    int Duration = 0,
    int Quality = 0,
    string? Description = null,
    long Timestamp = 0);
