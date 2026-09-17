namespace FunkArr.Messages.Enrichment;

public abstract record EnrichMoviesResponse;

public sealed record EnrichMoviesCompleted(
    EnrichedMovie[] Movies) : EnrichMoviesResponse;

public sealed record EnrichMoviesFailed(Exception Cause) : EnrichMoviesResponse;
