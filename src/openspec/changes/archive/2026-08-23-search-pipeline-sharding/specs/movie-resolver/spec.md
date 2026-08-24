# MovieResolver

Singleton persistent actor (T2, with snapshots) that handles TMDB lookups: find by imdbId or search by title. Provides 24-hour caching and request coalescing.

## ADDED Requirements

### Requirement: TMDB movie resolution by imdbId

MovieResolver SHALL handle `ResolveMovie(imdbId?, searchTerm?)` requests with an `imdbId` by querying the TMDB find-by-external-id endpoint and responding with `MovieResolved(movieInfo)`.

#### Scenario: Resolution by imdbId
- **WHEN** MovieResolver receives a `ResolveMovie` request with an `imdbId`
- **THEN** it SHALL query the TMDB API using the imdbId and respond with `MovieResolved` containing the movie information

### Requirement: TMDB movie resolution by search term

MovieResolver SHALL handle `ResolveMovie(imdbId?, searchTerm?)` requests with a `searchTerm` (and no `imdbId`) by querying the TMDB search endpoint and responding with `MovieResolved(movieInfo)`.

#### Scenario: Resolution by search term
- **WHEN** MovieResolver receives a `ResolveMovie` request with a `searchTerm` and no `imdbId`
- **THEN** it SHALL query the TMDB search API using the search term and respond with `MovieResolved` containing the best matching movie information

### Requirement: 24-hour cache

MovieResolver SHALL cache resolved movie data keyed by `imdbId` or search term with a 24-hour TTL. Cached entries MUST be served without making a TMDB API call.

#### Scenario: Cache hit by imdbId within 24 hours
- **WHEN** a `ResolveMovie` request arrives for an `imdbId` that was resolved less than 24 hours ago
- **THEN** MovieResolver SHALL return the cached result without calling the TMDB API

#### Scenario: Cache hit by search term within 24 hours
- **WHEN** a `ResolveMovie` request arrives for a `searchTerm` that was resolved less than 24 hours ago
- **THEN** MovieResolver SHALL return the cached result without calling the TMDB API

#### Scenario: Cache expiry after 24 hours
- **WHEN** a `ResolveMovie` request arrives for a key that was resolved 24 hours ago or longer
- **THEN** MovieResolver SHALL query the TMDB API again and update the cache

### Requirement: Request coalescing

Concurrent requests for the same key (`imdbId` or search term) SHALL result in only one TMDB API call. All waiting callers MUST receive the same result.

#### Scenario: Concurrent request coalescing
- **WHEN** multiple `ResolveMovie` requests for the same key arrive while an API call is in progress
- **THEN** only one TMDB API call SHALL be made and all callers SHALL receive the same `MovieResolved` response

### Requirement: Persistence identity

MovieResolver MUST use `PersistenceId` value `"movie-resolver"`.

#### Scenario: Persistence identity
- **WHEN** MovieResolver starts or recovers
- **THEN** it SHALL use `"movie-resolver"` as its PersistenceId

### Requirement: Event sourcing

MovieResolver SHALL persist `MovieResolvedEvent` events when a movie is resolved from the TMDB API.

#### Scenario: Event persistence on resolution
- **WHEN** MovieResolver successfully resolves a movie from the TMDB API
- **THEN** it SHALL persist a `MovieResolvedEvent` before responding to callers

### Requirement: Snapshot support

MovieResolver SHALL support snapshots using `MovieResolverSnapshot` to enable fast recovery. Snapshots MUST capture the full cache state.

#### Scenario: Snapshot save
- **WHEN** the snapshot criteria are met (e.g., after N events)
- **THEN** MovieResolver SHALL save a `MovieResolverSnapshot` containing the current cache state

#### Scenario: Recovery from snapshot
- **WHEN** MovieResolver restarts and a snapshot exists
- **THEN** it SHALL recover from the latest `MovieResolverSnapshot` and replay subsequent events

### Requirement: Singleton lifecycle

MovieResolver MUST be started as a singleton actor during application setup.

#### Scenario: Singleton startup
- **WHEN** the actor system starts
- **THEN** exactly one MovieResolver instance SHALL be created and registered
