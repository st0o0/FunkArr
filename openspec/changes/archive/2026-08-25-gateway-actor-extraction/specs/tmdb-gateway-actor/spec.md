## ADDED Requirements

### Requirement: TmdbGatewayActor singleton registration
The system SHALL provide a `TmdbGatewayActor` registered via Akka.Hosting as a singleton `ReceivePersistentActor` with PersistenceId `"tmdb-gateway"`. The actor SHALL own all TMDB API access, replacing direct `TmdbClient` usage in `MovieActor`.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the `TmdbGatewayActor` SHALL be registered in the ActorSystem and resolvable via `IActorRegistry`

#### Scenario: Single point of TMDB access
- **WHEN** any actor needs TMDB data
- **THEN** it SHALL request it from `TmdbGatewayActor` via Ask pattern, never calling `TmdbClient` directly

### Requirement: Event-sourced movie info caching
The `TmdbGatewayActor` SHALL cache TMDB movie metadata (title, original title, release year, runtime) as event-sourced state. When a movie is looked up, the result SHALL be persisted as a `MovieInfoCached` event. On recovery, the actor SHALL replay all events to rebuild its cache.

#### Scenario: First lookup by IMDB ID
- **WHEN** `GetMovieInfo(imdbId: "tt0082096")` arrives and no cached entry exists
- **THEN** the actor SHALL call `TmdbClient.FindByImdbIdAsync("tt0082096")`, persist a `MovieInfoCached` event, and reply with `MovieInfoResponse`

#### Scenario: Cached lookup (fresh)
- **WHEN** `GetMovieInfo(imdbId: "tt0082096")` arrives and a cached entry exists with age < 24 hours
- **THEN** the actor SHALL reply immediately from cache without calling the TMDB API

#### Scenario: Cached lookup (expired)
- **WHEN** `GetMovieInfo(imdbId: "tt0082096")` arrives and a cached entry exists with age >= 24 hours
- **THEN** the actor SHALL re-fetch from the TMDB API, persist a new `MovieInfoCached` event, and reply with fresh data

#### Scenario: Recovery from journal
- **WHEN** the actor restarts and replays its event journal
- **THEN** the cache SHALL be fully reconstructed from `MovieInfoCached` events without any TMDB API calls

### Requirement: Movie search by text query
The `TmdbGatewayActor` SHALL handle `SearchMovie(Query)` messages by searching TMDB and caching the result keyed by the resolved IMDB ID (if available) or search term.

#### Scenario: Text search resolves to movie
- **WHEN** `SearchMovie(query: "Das Boot")` arrives
- **THEN** the actor SHALL call `TmdbClient.SearchMovieAsync("Das Boot")`, persist a `MovieInfoCached` event if successful, and reply with `MovieInfoResponse`

#### Scenario: Text search returns no results
- **WHEN** `SearchMovie(query: "xyznonexistent")` arrives and TMDB returns no results
- **THEN** the actor SHALL reply with `MovieInfoResponse` containing null/empty values and NOT persist an event

### Requirement: Request deduplication
The `TmdbGatewayActor` SHALL deduplicate concurrent requests for the same IMDB ID, using the same waiter-list pattern as `TvdbGatewayActor`.

#### Scenario: Two concurrent requests for same movie
- **WHEN** `GetMovieInfo("tt0082096")` arrives from ActorA while a TMDB API call for "tt0082096" is already inflight
- **THEN** ActorA SHALL be added to the waiter list, and both actors SHALL receive the response when the API call completes

### Requirement: No snapshots
The `TmdbGatewayActor` SHALL NOT use snapshot persistence. State SHALL be reconstructed purely from event replay.

#### Scenario: Recovery without snapshots
- **WHEN** the actor recovers with journaled events
- **THEN** it SHALL replay all events to reconstruct state

### Requirement: TMDB API failure handling
The `TmdbGatewayActor` SHALL handle TMDB API failures gracefully, replying with null results and logging warnings. Failed lookups SHALL NOT be cached.

#### Scenario: API returns error
- **WHEN** `TmdbClient.FindByImdbIdAsync` returns null
- **THEN** the actor SHALL reply with empty `MovieInfoResponse` and no event SHALL be persisted

#### Scenario: No API key configured
- **WHEN** no TMDB API key is configured
- **THEN** all lookup methods SHALL reply with empty results without making HTTP requests

### Requirement: Persistence event schema
The `TmdbGatewayActor` SHALL use exactly one event type: `MovieInfoCached(ImdbId, Title, OriginalTitle, ReleaseYear, RuntimeMinutes, CachedAtUtcTicks)`.

#### Scenario: MovieInfoCached event content
- **WHEN** a successful lookup for "tt0082096" returns title "Das Boot", original title "Das Boot", year 1981, runtime 149
- **THEN** the persisted event SHALL contain `MovieInfoCached("tt0082096", "Das Boot", "Das Boot", 1981, 149, <current ticks>)`
