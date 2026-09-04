# tmdb-client

## Purpose

TMDB (The Movie Database) v3 API client for looking up movie metadata — details, alternative titles, and IMDB-to-TMDB ID resolution.

## Requirements

### Requirement: TmdbOptions configuration
The system SHALL define a `TmdbOptions` class bound to the `FunkArr:Tmdb` configuration section. It SHALL expose `ApiKey` (string, nullable) for the TMDB v3 API key. When `ApiKey` is null or empty, the TMDB client SHALL be disabled and all lookups SHALL return empty results.

#### Scenario: API key configured
- **WHEN** `FunkArr__Tmdb__ApiKey` environment variable is set to a valid key
- **THEN** the TmdbOptions.ApiKey property SHALL contain that key

#### Scenario: No API key configured
- **WHEN** no TMDB API key is configured
- **THEN** TmdbOptions.ApiKey SHALL be null and the TMDB client SHALL skip all API calls

### Requirement: TMDB v3 API authentication
The TMDB client SHALL authenticate with the TMDB v3 API at `https://api.themoviedb.org` using API key query parameter authentication. All requests SHALL include `?api_key={apiKey}` as a query parameter.

#### Scenario: Authenticated request
- **WHEN** the TMDB client makes an API call
- **THEN** the URL SHALL include `?api_key={apiKey}` as a query parameter

### Requirement: Fetch movie details by TMDB ID
The TMDB client SHALL expose a method to fetch movie details by TMDB ID. It SHALL request `GET /3/movie/{id}` and map the response to a `TmdbMovie` record containing: Id (int), Title (string), OriginalTitle (string), ReleaseDate (string?), Runtime (int?).

#### Scenario: Fetch existing movie
- **WHEN** `GetMovieAsync(tmdbId: 550)` is called
- **THEN** the client SHALL request `/3/movie/550?api_key=...` and return a TmdbMovie with title, release date, and runtime

#### Scenario: Movie not found
- **WHEN** the TMDB API returns 404 for a movie ID
- **THEN** the client SHALL return null

### Requirement: Find movie by IMDB ID
The TMDB client SHALL expose a method to find a movie by IMDB ID. It SHALL request `GET /3/find/{imdb_id}?external_source=imdb_id` and return the first movie result if any.

#### Scenario: IMDB ID maps to TMDB movie
- **WHEN** `FindByImdbIdAsync("tt0806910")` is called
- **THEN** the client SHALL request `/3/find/tt0806910?external_source=imdb_id&api_key=...` and return the matching TmdbMovie

#### Scenario: IMDB ID not found
- **WHEN** the TMDB API returns no movie results for an IMDB ID
- **THEN** the client SHALL return null

### Requirement: Fetch alternative titles
The TMDB client SHALL expose a method to fetch alternative titles for a movie. It SHALL request `GET /3/movie/{id}/alternative_titles` and return an array of title strings.

#### Scenario: Movie with alternative titles
- **WHEN** `GetAlternativeTitlesAsync(tmdbId: 550)` is called
- **THEN** the client SHALL return all alternative titles including German-specific titles

#### Scenario: No alternative titles
- **WHEN** a movie has no alternative titles
- **THEN** the client SHALL return an empty array

### Requirement: IsConfigured property
The TMDB client SHALL expose an `IsConfigured` property that returns true when a valid API key is set, false otherwise.

#### Scenario: Configured check
- **WHEN** TmdbOptions.ApiKey is set
- **THEN** IsConfigured SHALL return true
