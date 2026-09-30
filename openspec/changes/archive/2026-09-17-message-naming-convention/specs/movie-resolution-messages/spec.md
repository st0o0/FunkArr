# movie-resolution-messages

## MODIFIED Requirements

### Requirement: Movie enrichment response types

`MovieEnrichmentResponse` abstract record SHALL be renamed to `EnrichMoviesResponse`. `MoviesEnriched` SHALL be renamed to `EnrichMoviesCompleted`. `MovieEnrichmentFailed` SHALL be renamed to `EnrichMoviesFailed`.

#### Scenario: EnrichMovies file
- **WHEN** `EnrichMovies.cs` is examined
- **THEN** it SHALL contain `EnrichMovies`, `EnrichMoviesResponse`, `EnrichMoviesCompleted`, `EnrichMoviesFailed`
