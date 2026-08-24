# SeriesResolver

## Purpose

Singleton persistent actor (T2, with snapshots) that handles TVDB lookups for show information and episodes. Provides 24-hour caching and request coalescing.

## Requirements

### Requirement: TVDB show and episode resolution

SeriesResolver SHALL handle `ResolveSeries(tvdbId, season?)` requests and respond with `SeriesResolved(showName, episodes)` containing the resolved show name and episode list from TVDB.

#### Scenario: Successful series resolution
- **WHEN** SeriesResolver receives a `ResolveSeries` request with a valid `tvdbId`
- **THEN** it SHALL query the TVDB API and respond with `SeriesResolved` containing the show name and episodes

#### Scenario: Season-scoped resolution
- **WHEN** SeriesResolver receives a `ResolveSeries` request with a `tvdbId` and a `season` parameter
- **THEN** it SHALL respond with `SeriesResolved` containing only episodes for the specified season

### Requirement: 24-hour cache

SeriesResolver SHALL cache resolved series data keyed by `tvdbId` with a 24-hour TTL. Cached entries MUST be served without making a TVDB API call.

#### Scenario: Cache hit within 24 hours
- **WHEN** a `ResolveSeries` request arrives for a `tvdbId` that was resolved less than 24 hours ago
- **THEN** SeriesResolver SHALL return the cached result without calling the TVDB API

#### Scenario: Cache expiry after 24 hours
- **WHEN** a `ResolveSeries` request arrives for a `tvdbId` that was resolved 24 hours ago or longer
- **THEN** SeriesResolver SHALL query the TVDB API again and update the cache

### Requirement: Request coalescing

Concurrent requests for the same `tvdbId` SHALL result in only one TVDB API call. All waiting callers MUST receive the same result.

#### Scenario: Concurrent request coalescing
- **WHEN** multiple `ResolveSeries` requests for the same `tvdbId` arrive while an API call is in progress
- **THEN** only one TVDB API call SHALL be made and all callers SHALL receive the same `SeriesResolved` response

### Requirement: Persistence identity

SeriesResolver MUST use `PersistenceId` value `"series-resolver"`.

#### Scenario: Persistence identity
- **WHEN** SeriesResolver starts or recovers
- **THEN** it SHALL use `"series-resolver"` as its PersistenceId

### Requirement: Event sourcing

SeriesResolver SHALL persist `SeriesResolvedEvent` events when a series is resolved from the TVDB API.

#### Scenario: Event persistence on resolution
- **WHEN** SeriesResolver successfully resolves a series from the TVDB API
- **THEN** it SHALL persist a `SeriesResolvedEvent` before responding to callers

### Requirement: Snapshot support

SeriesResolver SHALL support snapshots using `SeriesResolverSnapshot` to enable fast recovery. Snapshots MUST capture the full cache state.

#### Scenario: Snapshot save
- **WHEN** the snapshot criteria are met (e.g., after N events)
- **THEN** SeriesResolver SHALL save a `SeriesResolverSnapshot` containing the current cache state

#### Scenario: Recovery from snapshot
- **WHEN** SeriesResolver restarts and a snapshot exists
- **THEN** it SHALL recover from the latest `SeriesResolverSnapshot` and replay subsequent events

### Requirement: Singleton lifecycle

SeriesResolver MUST be started as a singleton actor during application setup.

#### Scenario: Singleton startup
- **WHEN** the actor system starts
- **THEN** exactly one SeriesResolver instance SHALL be created and registered
