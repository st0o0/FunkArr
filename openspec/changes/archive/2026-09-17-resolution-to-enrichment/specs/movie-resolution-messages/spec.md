## MODIFIED Requirements

### Requirement: ResolveMovie request message
FunkArr.Messages.MetadataResolver SHALL define an `EnrichMovies` sealed record containing: ImdbId (string?), TmdbId (int?), Candidates (MovieCandidate[]). At least one of ImdbId or TmdbId SHALL be non-null.

#### Scenario: EnrichMovies with TMDB ID
- **WHEN** a movie search produces a matched item with TmdbId=550
- **THEN** an `EnrichMovies(ImdbId=null, TmdbId=550, Candidates=[...])` message SHALL be constructed

#### Scenario: EnrichMovies with IMDB ID
- **WHEN** a movie search produces a matched item with ImdbId="tt0806910"
- **THEN** an `EnrichMovies(ImdbId="tt0806910", TmdbId=null, Candidates=[...])` message SHALL be constructed

### Requirement: MovieResolved record
FunkArr.Messages.MetadataResolver SHALL define an `EnrichedMovie` sealed record containing: Index (int), Title (string), Year (int), ImdbId (string?), TmdbId (int?), Confidence (float), Method (MatchMethod). EnrichedMovie SHALL implement IEnrichmentResult.

#### Scenario: Movie enriched via TMDB title match
- **WHEN** a movie is enriched via TMDB title similarity
- **THEN** EnrichedMovie SHALL have Title (TMDB title), Year (release year), TmdbId, Confidence=similarity, Method=MatchMethod.TitleMatch

#### Scenario: Movie enriched via year match
- **WHEN** a movie is enriched via year + weak title
- **THEN** EnrichedMovie SHALL have Method=MatchMethod.YearMatch and Confidence = similarity * 0.8

### Requirement: IMovieResolutionResponse marker interface
FunkArr.Messages.MetadataResolver SHALL define an `IMovieEnrichmentResponse` marker interface implemented by `MoviesEnriched` and `MovieEnrichmentFailed`.

#### Scenario: Response type discrimination
- **WHEN** the MovieSearchWorker receives an IMovieEnrichmentResponse
- **THEN** it SHALL pattern-match on MoviesEnriched or MovieEnrichmentFailed

### Requirement: MoviesResolved response message
FunkArr.Messages.MetadataResolver SHALL define a `MoviesEnriched` sealed record containing: Movies (EnrichedMovie[]). It SHALL implement `IMovieEnrichmentResponse`.

#### Scenario: Successful movie enrichment
- **WHEN** a movie candidate is enriched against TMDB
- **THEN** MoviesEnriched SHALL contain one EnrichedMovie entry

### Requirement: MovieResolutionFailed response message
FunkArr.Messages.MetadataResolver SHALL define a `MovieEnrichmentFailed` sealed record containing: Cause (Exception). It SHALL implement `IMovieEnrichmentResponse`.

#### Scenario: TMDB unavailable
- **WHEN** the TMDB API is unreachable
- **THEN** MovieEnrichmentFailed SHALL contain Cause describing the error
