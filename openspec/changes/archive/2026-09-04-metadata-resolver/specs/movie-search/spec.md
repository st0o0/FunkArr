## ADDED Requirements

### Requirement: MovieSearchWorker movie resolution stage
After receiving ScoreCompleted, the MovieSearchWorker SHALL check if the search has an ImdbId or TmdbId. If so, the worker SHALL construct MovieCandidates from the matched scored items and Ask the MetadataResolver to resolve the movie identity. On success, the worker SHALL enrich the SearchResultItems with validated title, year, and resolution metadata before calling ToScoredResult.

#### Scenario: Movie resolution with TMDB ID
- **WHEN** a movie search with TmdbId=550 produces matched items
- **THEN** the worker SHALL construct MovieCandidates and Ask MetadataResolver with ResolveMovie(TmdbId=550, ...)

#### Scenario: Movie resolution with IMDB ID
- **WHEN** a movie search with ImdbId="tt0806910" produces matched items
- **THEN** the worker SHALL construct MovieCandidates and Ask MetadataResolver with ResolveMovie(ImdbId="tt0806910", ...)

#### Scenario: No movie ID available
- **WHEN** a movie search has no ImdbId and no TmdbId (query-only search)
- **THEN** the worker SHALL skip movie resolution

#### Scenario: Resolution succeeds
- **WHEN** the MetadataResolver responds with MoviesResolved
- **THEN** the worker SHALL enrich the release title with validated year and title, and set ResolutionConfidence/ResolutionStrategy on the SearchResultItem

#### Scenario: Resolution fails
- **WHEN** the MetadataResolver responds with MovieResolutionFailed
- **THEN** the worker SHALL proceed with ToScoredResult using existing metadata (graceful fallback)

#### Scenario: Resolution times out
- **WHEN** the MetadataResolver Ask times out
- **THEN** the worker SHALL proceed with ToScoredResult using existing metadata

### Requirement: MovieSearchWorker uses IMetadataResolver
The MovieSearchWorker SHALL resolve the MetadataResolver singleton via `Context.GetActor<IMetadataResolver>()`.

#### Scenario: Actor resolution
- **WHEN** MovieSearchWorker is constructed
- **THEN** it SHALL resolve `IMetadataResolver` for movie resolution requests
