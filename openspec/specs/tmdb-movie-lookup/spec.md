## Purpose

TMDB API client that resolves IMDB IDs and movie text queries to German-localized movie metadata (title, original title, release year, runtime).

## Requirements

### Requirement: IMDB ID resolution to movie metadata
`TmdbClient` SHALL resolve an IMDB ID to movie metadata including the German title, original title, release year, and runtime in minutes via the TMDB API v3 `/find/{external_id}` endpoint followed by `/movie/{id}` for runtime.

#### Scenario: IMDB ID resolves to German title and runtime
- **WHEN** `TmdbClient.FindByImdbIdAsync("tt0108052")` is called and TMDB returns a match
- **THEN** the result SHALL contain the German title ("Schindlers Liste"), original title ("Schindler's List"), release year (1993), and runtime (195)

#### Scenario: IMDB ID not found in TMDB
- **WHEN** `TmdbClient.FindByImdbIdAsync("tt9999999")` is called and TMDB returns no movie results
- **THEN** the result SHALL be null

#### Scenario: TMDB API unreachable
- **WHEN** `TmdbClient.FindByImdbIdAsync` is called and the HTTP request fails
- **THEN** the result SHALL be null (no exception propagated to caller)

### Requirement: Text-based movie search via TMDB
`TmdbClient` SHALL search for movies by text query via the TMDB API v3 `/search/movie` endpoint with `language=de-DE`, returning the first (best) match's metadata.

#### Scenario: Text query resolves to movie metadata
- **WHEN** `TmdbClient.SearchMovieAsync("Schindlers Liste")` is called and TMDB returns results
- **THEN** the result SHALL contain metadata from the first result (German title, original title, release year) and runtime from a follow-up `/movie/{id}` call

#### Scenario: Text query returns no results
- **WHEN** `TmdbClient.SearchMovieAsync("xyznonexistent")` is called and TMDB returns empty results
- **THEN** the result SHALL be null

#### Scenario: TMDB API unreachable during text search
- **WHEN** `TmdbClient.SearchMovieAsync` is called and the HTTP request fails
- **THEN** the result SHALL be null (no exception propagated to caller)

### Requirement: German language preference
All TMDB API requests SHALL include `language=de-DE` to prefer German-localized metadata. If TMDB has no German title, the original title SHALL be used as fallback.

#### Scenario: German title available
- **WHEN** TMDB returns `title="Schindlers Liste"` and `original_title="Schindler's List"` for language=de-DE
- **THEN** `TmdbMovieInfo.Title` SHALL be "Schindlers Liste"

#### Scenario: No German title available (same as original)
- **WHEN** TMDB returns `title="Parasite"` and `original_title="기생충"` for language=de-DE
- **THEN** `TmdbMovieInfo.Title` SHALL be "Parasite" (TMDB returns the most common localized title)

### Requirement: API key authentication
`TmdbClient` SHALL authenticate all requests using the configured TMDB API key (v3) passed as the `api_key` query parameter.

#### Scenario: API key included in requests
- **WHEN** any TMDB API request is made
- **THEN** the request URL SHALL include `api_key={configured_key}` as a query parameter

#### Scenario: No API key configured
- **WHEN** `TmdbClient` is constructed without a valid API key (null or empty)
- **THEN** all lookup methods SHALL return null without making HTTP requests

### Requirement: Runtime resolution via movie detail endpoint
When a movie is found via `/find` or `/search/movie`, `TmdbClient` SHALL fetch the full movie details from `/movie/{id}` to obtain the runtime field, which is not included in find/search responses.

#### Scenario: Runtime fetched from movie details
- **WHEN** `/find/{imdb_id}` returns a movie with TMDB ID 424
- **THEN** `TmdbClient` SHALL call `/movie/424?language=de-DE` and include the `runtime` field in the result

#### Scenario: Movie detail fetch fails
- **WHEN** `/find/{imdb_id}` succeeds but the subsequent `/movie/{id}` call fails
- **THEN** the result SHALL still be returned with `RuntimeMinutes = null`
