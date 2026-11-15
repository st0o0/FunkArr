## MODIFIED Requirements

### Requirement: Fetch movie details by TMDB ID
The TMDB client SHALL expose a method to fetch movie details by TMDB ID. It SHALL request `GET /3/movie/{id}` and map the response to a `TmdbMovie` record containing: Id (int), Title (string), OriginalTitle (string), ReleaseDate (string?), Runtime (int?). The client SHALL distinguish between "not found" (404 or empty result) and transient HTTP errors (429, 5xx). Not-found SHALL return null. Transient errors SHALL throw `HttpRequestException` so callers can classify the failure.

#### Scenario: Fetch existing movie
- **WHEN** `GetMovieAsync(tmdbId: 550)` is called
- **THEN** the client SHALL request `/3/movie/550?api_key=...` and return a TmdbMovie with title, release date, and runtime

#### Scenario: Movie not found (404)
- **WHEN** the TMDB API returns 404 for a movie ID
- **THEN** the client SHALL return null

#### Scenario: Transient HTTP error (429)
- **WHEN** the TMDB API returns 429 Too Many Requests
- **THEN** the client SHALL throw `HttpRequestException` (the resilience handler may retry before the throw reaches the caller)

#### Scenario: Transient HTTP error (5xx)
- **WHEN** the TMDB API returns 500 or 503
- **THEN** the client SHALL throw `HttpRequestException`

### Requirement: TMDB v3 API authentication
The TMDB client SHALL authenticate with the TMDB v3 API at `https://api.themoviedb.org` using API key query parameter authentication. All requests SHALL include `?api_key={apiKey}` as a query parameter. The API key SHALL NOT appear in any log output. When logging request failures, only the URL path SHALL be logged, not the query string.

#### Scenario: Authenticated request
- **WHEN** the TMDB client makes an API call
- **THEN** the URL SHALL include `?api_key={apiKey}` as a query parameter

#### Scenario: API key not logged on failure
- **WHEN** a TMDB API request fails and the client logs a warning
- **THEN** the log message SHALL contain only the URL path (e.g., `movie/550`), not the query string containing the API key

#### Scenario: API key not logged on success
- **WHEN** a TMDB API request succeeds and the client logs debug information
- **THEN** no log message SHALL contain the API key value
