# Movie Resolution Messages

## Purpose

Defines the command, response, and intermediate record types for movie metadata enrichment. These messages flow between MovieSearchWorker and the movie enrichment subsystem to enrich Mediathek movie matches against TMDB metadata.

## Requirements

### Requirement: ResolveMovie request message
FunkArr.Messages.MetadataResolver SHALL define an `EnrichMovies` sealed record containing: ImdbId (string?), TmdbId (int?), Candidates (MovieCandidate[]). At least one of ImdbId or TmdbId SHALL be non-null.

#### Scenario: EnrichMovies with TMDB ID
- **WHEN** a movie search produces a matched item with TmdbId=550
- **THEN** an `EnrichMovies(ImdbId=null, TmdbId=550, Candidates=[...])` message SHALL be constructed

#### Scenario: EnrichMovies with IMDB ID
- **WHEN** a movie search produces a matched item with ImdbId="tt0806910"
- **THEN** an `EnrichMovies(ImdbId="tt0806910", TmdbId=null, Candidates=[...])` message SHALL be constructed

### Requirement: MovieCandidate record
FunkArr.Messages.MetadataResolver SHALL define a `MovieCandidate` sealed record containing: Index (int), Title (string), AiredAt (DateTimeOffset?), Duration (int). The Index field SHALL correspond to the original ScoredItem index for result correlation.

#### Scenario: Movie candidate from Mediathek
- **WHEN** a Mediathek item "Das Boot" is matched as a movie
- **THEN** the MovieCandidate SHALL have Title="Das Boot", AiredAt from the Mediathek timestamp, Duration in seconds

### Requirement: MovieResolved record
FunkArr.Messages.MetadataResolver SHALL define an `EnrichedMovie` sealed record containing: Index (int), Title (string), Year (int), ImdbId (string?), TmdbId (int?), Confidence (float), Method (MatchMethod). EnrichedMovie SHALL implement IEnrichmentResult.

#### Scenario: Movie enriched via TMDB title match
- **WHEN** a movie is enriched via TMDB title similarity
- **THEN** EnrichedMovie SHALL have Title (TMDB title), Year (release year), TmdbId, Confidence=similarity, Method=MatchMethod.TitleMatch

#### Scenario: Movie enriched via year match
- **WHEN** a movie is enriched via year + weak title
- **THEN** EnrichedMovie SHALL have Method=MatchMethod.YearMatch and Confidence = similarity * 0.8

### Requirement: IMovieResolutionResponse marker interface
FunkArr.Messages.MetadataResolver SHALL define an `abstract record MovieEnrichmentResponse` as the base for all movie enrichment responses, replacing the `IMovieEnrichmentResponse` marker interface. `MoviesEnriched` and `MovieEnrichmentFailed` SHALL extend `MovieEnrichmentResponse`.

#### Scenario: Response type discrimination
- **WHEN** the MovieSearchWorker receives a `MovieEnrichmentResponse`
- **THEN** it SHALL pattern-match on `MoviesEnriched` or `MovieEnrichmentFailed`

### Requirement: MoviesResolved response message
FunkArr.Messages.MetadataResolver SHALL define a `MoviesEnriched` sealed record containing: Movies (EnrichedMovie[]). It SHALL extend `MovieEnrichmentResponse`.

#### Scenario: Successful movie enrichment
- **WHEN** a movie candidate is enriched against TMDB
- **THEN** MoviesEnriched SHALL contain one EnrichedMovie entry

### Requirement: MovieResolutionFailed response message
FunkArr.Messages.MetadataResolver SHALL define a `MovieEnrichmentFailed` sealed record containing: Cause (Exception). It SHALL extend `MovieEnrichmentResponse`.

#### Scenario: TMDB unavailable
- **WHEN** the TMDB API is unreachable
- **THEN** MovieEnrichmentFailed SHALL contain Cause describing the error
