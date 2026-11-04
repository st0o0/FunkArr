namespace FunkArr.Messages.Scoring;

public sealed record TestScoreWithEnrichment(
    Guid TestScoringId,
    MatchingConfig Config,
    ScoreCandidate[] Candidates,
    MediaType? MediaType = null,
    int? TvdbId = null,
    int? TmdbId = null,
    string? ImdbId = null,
    Enrichment.EnrichmentConfig? EnrichmentConfig = null) : IWithTestScoringId;

public abstract record TestScoreWithEnrichmentResponse;

public sealed record TestScoreWithEnrichmentCompleted(
    History.ItemTrace[] ItemTraces) : TestScoreWithEnrichmentResponse;

public sealed record TestScoreWithEnrichmentFailed(
    Exception Cause) : TestScoreWithEnrichmentResponse;
