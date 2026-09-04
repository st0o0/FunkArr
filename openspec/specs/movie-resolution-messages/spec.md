# Movie Resolution Messages

## Purpose

Defines the command, response, and intermediate record types for movie metadata resolution. These messages flow between MovieSearchWorker and the movie resolution subsystem (MovieResolutionManager/Worker) to resolve Mediathek movie matches against TMDB metadata.

## Requirements

### Requirement: ResolveMovie request message
FunkArr.Messages.MetadataResolver SHALL define a `ResolveMovie` sealed record containing: ImdbId (string?), TmdbId (int?), Candidates (MovieCandidate[]). At least one of ImdbId or TmdbId SHALL be non-null.

#### Scenario: ResolveMovie with TMDB ID
- **WHEN** a movie search produces a matched item with TmdbId=550
- **THEN** a `ResolveMovie(ImdbId=null, TmdbId=550, Candidates=[...])` message SHALL be constructed

#### Scenario: ResolveMovie with IMDB ID
- **WHEN** a movie search produces a matched item with ImdbId="tt0806910"
- **THEN** a `ResolveMovie(ImdbId="tt0806910", TmdbId=null, Candidates=[...])` message SHALL be constructed

### Requirement: MovieCandidate record
FunkArr.Messages.MetadataResolver SHALL define a `MovieCandidate` sealed record containing: Index (int), Title (string), AiredAt (DateTimeOffset?), Duration (int). The Index field SHALL correspond to the original ScoredItem index for result correlation.

#### Scenario: Movie candidate from Mediathek
- **WHEN** a Mediathek item "Das Boot" is matched as a movie
- **THEN** the MovieCandidate SHALL have Title="Das Boot", AiredAt from the Mediathek timestamp, Duration in seconds

### Requirement: MovieResolved record
FunkArr.Messages.MetadataResolver SHALL define a `MovieResolved` sealed record containing: Index (int), Title (string), Year (int), ImdbId (string?), TmdbId (int?), Confidence (float), Strategy (string).

#### Scenario: Movie resolved via TMDB ID
- **WHEN** a movie is resolved via TMDB direct lookup
- **THEN** MovieResolved SHALL have Title (TMDB title), Year (release year), TmdbId, Confidence=1.0, Strategy="TmdbIdLookup"

#### Scenario: Movie resolved via IMDB ID
- **WHEN** a movie is resolved via IMDB → TMDB find
- **THEN** MovieResolved SHALL have ImdbId (original), TmdbId (found), Confidence=0.95, Strategy="ImdbIdLookup"

### Requirement: IMovieResolutionResponse marker interface
FunkArr.Messages.MetadataResolver SHALL define an `IMovieResolutionResponse` marker interface implemented by `MoviesResolved` and `MovieResolutionFailed`.

#### Scenario: Response type discrimination
- **WHEN** the MovieSearchWorker receives an IMovieResolutionResponse
- **THEN** it SHALL pattern-match on MoviesResolved or MovieResolutionFailed

### Requirement: MoviesResolved response message
FunkArr.Messages.MetadataResolver SHALL define a `MoviesResolved` sealed record containing: Movies (MovieResolved[]). It SHALL implement `IMovieResolutionResponse`.

#### Scenario: Successful movie resolution
- **WHEN** a movie candidate is resolved against TMDB
- **THEN** MoviesResolved SHALL contain one MovieResolved entry

### Requirement: MovieResolutionFailed response message
FunkArr.Messages.MetadataResolver SHALL define a `MovieResolutionFailed` sealed record containing: Reason (string). It SHALL implement `IMovieResolutionResponse`.

#### Scenario: TMDB unavailable
- **WHEN** the TMDB API is unreachable
- **THEN** MovieResolutionFailed SHALL contain Reason describing the error
