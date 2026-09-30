## ADDED Requirements

### Requirement: TvdbGatewayActor singleton registration
The system SHALL provide a `TvdbGatewayActor` registered via Akka.Hosting as a singleton `ReceivePersistentActor` with PersistenceId `"tvdb-gateway"`. The actor SHALL own all TVDB API access, replacing direct `TvdbClient` usage in `ShowActor`.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the `TvdbGatewayActor` SHALL be registered in the ActorSystem and resolvable via `IActorRegistry`

#### Scenario: Single point of TVDB access
- **WHEN** any actor needs TVDB data
- **THEN** it SHALL request it from `TvdbGatewayActor` via Ask pattern, never calling `TvdbClient` directly

### Requirement: Event-sourced show info caching
The `TvdbGatewayActor` SHALL cache TVDB show metadata (name, German name, aliases) as event-sourced state. When a show is looked up, the result SHALL be persisted as a `ShowInfoCached` event. On recovery, the actor SHALL replay all events to rebuild its cache without making API calls.

#### Scenario: First lookup for a show
- **WHEN** `GetShowInfo(tvdbId: 83214)` arrives and no cached entry exists
- **THEN** the actor SHALL call `TvdbClient.GetShowAsync(83214)`, persist a `ShowInfoCached` event with the result, and reply with `ShowInfoResponse`

#### Scenario: Cached lookup (fresh)
- **WHEN** `GetShowInfo(tvdbId: 83214)` arrives and a cached entry exists with age < 24 hours
- **THEN** the actor SHALL reply immediately from cache without calling the TVDB API

#### Scenario: Cached lookup (expired)
- **WHEN** `GetShowInfo(tvdbId: 83214)` arrives and a cached entry exists with age >= 24 hours
- **THEN** the actor SHALL re-fetch from the TVDB API, persist a new `ShowInfoCached` event (overwriting the old entry in state), and reply with the fresh data

#### Scenario: Recovery from journal
- **WHEN** the actor restarts and replays its event journal
- **THEN** the cache SHALL be fully reconstructed from journaled `ShowInfoCached` events without any TVDB API calls

#### Scenario: Multiple ShowInfoCached events for same tvdbId
- **WHEN** the journal contains two `ShowInfoCached` events for tvdbId 83214 (from initial lookup and TTL refresh)
- **THEN** the state SHALL contain only the latest entry (dictionary assignment semantics)

### Requirement: Event-sourced episode caching
The `TvdbGatewayActor` SHALL cache TVDB episode lists per (tvdbId, season) as event-sourced state. Episode lookups SHALL be persisted as `EpisodesCached` events.

#### Scenario: First episode lookup for a season
- **WHEN** `GetEpisodes(tvdbId: 83214, season: 1)` arrives and no cached episodes exist for that season
- **THEN** the actor SHALL call `TvdbClient.GetEpisodesAsync(83214, 1)`, persist an `EpisodesCached` event, and reply with `EpisodesResponse`

#### Scenario: Cached episodes (fresh)
- **WHEN** `GetEpisodes(tvdbId: 83214, season: 1)` arrives and cached episodes exist with age < 24 hours
- **THEN** the actor SHALL reply immediately from cache

#### Scenario: Different seasons cached independently
- **WHEN** season 1 episodes are cached but season 3 is requested
- **THEN** the actor SHALL fetch season 3 from the API, persist an `EpisodesCached` event for season 3, and reply — season 1 cache remains unaffected

### Requirement: Combined ResolveShow message
The `TvdbGatewayActor` SHALL handle a `ResolveShow(TvdbId, Season?)` convenience message that returns both show info and optionally episodes in a single response.

#### Scenario: ResolveShow with season
- **WHEN** `ResolveShow(tvdbId: 83214, season: 1)` arrives
- **THEN** the actor SHALL resolve show info and season 1 episodes (from cache or API) and reply with `ShowResolved(Name, GermanName, Aliases, Episodes)`

#### Scenario: ResolveShow without season
- **WHEN** `ResolveShow(tvdbId: 83214, season: null)` arrives
- **THEN** the actor SHALL resolve show info only and reply with `ShowResolved(Name, GermanName, Aliases, Episodes: null)`

#### Scenario: Show info cached but episodes not
- **WHEN** show info is cached (fresh) but the requested season's episodes are not
- **THEN** the actor SHALL use cached show info and fetch only the episodes

### Requirement: Request deduplication
The `TvdbGatewayActor` SHALL deduplicate concurrent requests for the same tvdbId. If a second request arrives while a TVDB API call is inflight for the same tvdbId, the sender SHALL be added to a waiter list and notified when the response arrives.

#### Scenario: Two concurrent requests for same show
- **WHEN** `GetShowInfo(83214)` arrives from ActorA while a TVDB API call for 83214 is already inflight (triggered by ActorB)
- **THEN** ActorA SHALL be added to the waiter list, and when the API response arrives, both ActorA and ActorB SHALL receive the `ShowInfoResponse`

#### Scenario: Concurrent requests for different shows
- **WHEN** `GetShowInfo(83214)` is inflight and `GetShowInfo(153241)` arrives
- **THEN** the actor SHALL initiate a separate API call for 153241 (no dedup across different keys)

#### Scenario: Episode request dedup
- **WHEN** two `GetEpisodes(83214, season: 1)` requests arrive concurrently
- **THEN** only one API call SHALL be made, and both senders SHALL receive the response

### Requirement: No snapshots
The `TvdbGatewayActor` SHALL NOT use snapshot persistence. State SHALL be reconstructed purely from event replay.

#### Scenario: Recovery without snapshots
- **WHEN** the actor recovers after restart with 500 journaled events
- **THEN** it SHALL replay all 500 events to reconstruct state, without loading a snapshot

### Requirement: TVDB API failure handling
The `TvdbGatewayActor` SHALL handle TVDB API failures gracefully, replying with null/empty results and logging warnings.

#### Scenario: API returns error status
- **WHEN** `TvdbClient.GetShowAsync` returns null (API error)
- **THEN** the actor SHALL reply with `ShowInfoResponse` containing empty/default values and log a warning

#### Scenario: API timeout
- **WHEN** the TVDB API call times out
- **THEN** the actor SHALL reply with empty results, notify all waiters, and log a warning

#### Scenario: Failed lookup not cached
- **WHEN** a TVDB API call fails
- **THEN** no `ShowInfoCached` event SHALL be persisted (the next request will retry)

### Requirement: Persistence event schema
The `TvdbGatewayActor` SHALL use exactly two event types: `ShowInfoCached(TvdbId, Name, GermanName, Aliases[], CachedAtUtcTicks)` and `EpisodesCached(TvdbId, Season, Episodes[], CachedAtUtcTicks)`.

#### Scenario: ShowInfoCached event content
- **WHEN** a successful show lookup for tvdbId 83214 returns name "Tatort", German name "Tatort", aliases ["Tatort aus Österreich"]
- **THEN** the persisted event SHALL contain `ShowInfoCached(83214, "Tatort", "Tatort", ["Tatort aus Österreich"], <current ticks>)`

#### Scenario: EpisodesCached event content
- **WHEN** a successful episode lookup for tvdbId 83214, season 1 returns 12 episodes
- **THEN** the persisted event SHALL contain `EpisodesCached(83214, 1, <12 TvdbEpisodeInfo records>, <current ticks>)`
