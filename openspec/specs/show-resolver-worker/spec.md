> **ARCHIVED**: Split into `series-resolver` and `movie-resolver`.

## Purpose

ShowResolverWorker is a persistent child actor of SearchCoordinator that resolves TVDB and TMDB metadata, maintaining an in-memory cache backed by event-sourced persistence with inflight request coalescing.

## Requirements

### Requirement: Show resolution via Ask
`ShowResolverWorker` SHALL respond to `ResolveTvdb(int tvdbId)` with `TvdbResolved(ShowInfo)` and `ResolveTmdb(string imdbId)` with `TmdbResolved(MovieInfo)`. It SHALL use `TvdbClient` and `TmdbClient` internally.

#### Scenario: TVDB resolution
- **WHEN** `ShowResolverWorker` receives `ResolveTvdb(370761)`
- **THEN** it SHALL query `TvdbClient.GetShowAsync(370761)` and reply with `TvdbResolved(showInfo)` containing show name and episode list

#### Scenario: TMDB resolution by IMDB ID
- **WHEN** `ShowResolverWorker` receives `ResolveTmdb("tt0108052")`
- **THEN** it SHALL query `TmdbClient.FindByImdbIdAsync("tt0108052")` and reply with `TmdbResolved(movieInfo)` containing title, original title, and runtime

#### Scenario: Resolution failure
- **WHEN** `TvdbClient` returns null for a given TVDB ID
- **THEN** `ShowResolverWorker` SHALL reply with `TvdbResolved(null)`

### Requirement: In-memory cache with inflight coalescing
`ShowResolverWorker` SHALL maintain an in-memory `Dictionary<id, ShowInfo>` cache. Concurrent requests for the same ID SHALL be coalesced — only one HTTP call per ID.

#### Scenario: Cache hit
- **WHEN** `ShowResolverWorker` receives `ResolveTvdb(370761)` and the cache contains an entry for 370761
- **THEN** it SHALL reply immediately from cache without making an HTTP call

#### Scenario: Inflight coalescing
- **WHEN** two `ResolveTvdb(370761)` requests arrive before the first HTTP call completes
- **THEN** only one HTTP call SHALL be made and both callers SHALL receive the result

### Requirement: Tier 2 event-sourced persistence
`ShowResolverWorker` SHALL be a `ReceivePersistentActor` with `PersistenceId: "show-resolver"`. It SHALL persist `ShowResolved(tvdbId, showInfo)` and `MovieResolved(imdbId, movieInfo)` events. Snapshots SHALL be taken every 500 events.

#### Scenario: Cache warm on recovery
- **WHEN** `ShowResolverWorker` restarts after a crash
- **THEN** it SHALL recover its cache from the latest snapshot + replayed events, without making HTTP calls for previously resolved shows

#### Scenario: Snapshot every 500 events
- **WHEN** 500 resolution events have been persisted since the last snapshot
- **THEN** `ShowResolverWorker` SHALL save a snapshot of the current cache state

#### Scenario: TTL enforcement during recovery
- **WHEN** a recovered event is older than the cache TTL
- **THEN** `ShowResolverWorker` SHALL skip that entry during recovery

## REMOVED Requirements

### Requirement: ShowResolverWorker persistent actor
**Reason**: Split into two independent singleton persistent actors: `SeriesResolver` (TVDB) and `MovieResolver` (TMDB). Each has its own persistence stream, cache, and request coalescing -- eliminating the mixed-concern design.
**Migration**: PersistenceId `show-resolver` is abandoned. New PersistenceIds `series-resolver` and `movie-resolver` start with empty journals. Caches rebuild organically within 24h from API calls. Acceptable at version 0.x (no data migration needed).
