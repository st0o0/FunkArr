## Why

When Radarr sends a movie search request with only an IMDB ID and no query string, `MovieSearchActor` searches the Mediathek with an empty string and returns nothing useful. Even with a query, the English title often fails to match Mediathek content (e.g., "The Lives of Others" won't find "Das Leben der Anderen"). Adding TMDB integration resolves IMDB IDs to German titles and provides runtime data for duration-based filtering — making movie search actually functional for the Radarr use case.

## What Changes

- Add `TmdbClient` HTTP client that resolves IMDB IDs and text queries to movie metadata (German title, original title, runtime) via TMDB API v3
- Enhance `MovieSearchActor` to use TMDB data: search Mediathek with the resolved German title and populate `MatchContext.ExpectedDurationSeconds` from runtime
- Add `TmdbApiKey` to `SearchOptions` configuration (required for TMDB API access)
- Update `ExecuteMovieSearch` routing message to carry resolved movie metadata

## Capabilities

### New Capabilities
- `tmdb-movie-lookup`: TMDB API client that resolves IMDB IDs and movie queries to German titles + runtime metadata

### Modified Capabilities
- `search-routing`: `MovieSearchActor` gains TMDB-based title resolution and duration-aware filtering instead of pass-through text search

## Impact

- **Code**: `FunkArr.Search/` — new `TmdbClient.cs`, modified `MovieSearchActor.cs`, updated `SearchActor.cs` (passes `TmdbClient` to child), updated `SearchRoutingMessages.cs`
- **Configuration**: New `SearchOptions.TmdbApiKey` property, environment variable `FunkArr__Search__TmdbApiKey`
- **Dependencies**: No new NuGet packages (uses existing `HttpClient` + `System.Text.Json`)
- **External**: TMDB API v3 (free, requires API key registration at themoviedb.org)
- **Tests**: New `TmdbClientTests`, updated `MovieSearchActorTests`
