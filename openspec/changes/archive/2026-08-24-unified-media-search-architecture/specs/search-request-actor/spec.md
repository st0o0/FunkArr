## ADDED Requirements

### Requirement: SearchRequestActor sharded stateless actor
The system SHALL provide a `SearchRequestActor` as a Cluster Sharding entity that handles all search types (TV, Movie, Text) in a single ShardRegion. The actor SHALL be stateless (no persistence) and passivate after 15 minutes of inactivity.

#### Scenario: TV search entity key
- **WHEN** a `Search.Tv` message arrives with tvdbId 329324
- **THEN** the EntityKey SHALL be `"tv:329324"`

#### Scenario: Movie search entity key
- **WHEN** a `Search.Movie` message arrives with imdbId "tt0082096"
- **THEN** the EntityKey SHALL be `"movie:tt0082096"`

#### Scenario: Movie search entity key from query
- **WHEN** a `Search.Movie` message arrives with imdbId null and query "Das Boot"
- **THEN** the EntityKey SHALL be `"movie:q:Das Boot"`

#### Scenario: Text search entity key
- **WHEN** a `Search.Text` message arrives with query "Tatort"
- **THEN** the EntityKey SHALL be `"text:Tatort"`

#### Scenario: Passivation
- **WHEN** the actor receives no messages for 15 minutes
- **THEN** it SHALL passivate

### Requirement: Unified two-phase search protocol for TV and Movie
The `SearchRequestActor` SHALL implement a two-phase protocol for TV and Movie searches: (1) resolve search hints from the media actor, (2) query MediathekGateway using the hints, then delegate matching back to the media actor.

#### Scenario: TV search full flow
- **WHEN** a `Search.Tv(tvdbId=329324, showName="Feuer & Flamme", season=1, episode=null)` arrives
- **THEN** the actor SHALL ask `ShowActor(329324).ResolveSearch`, build a `MediathekSearchQuery` from the returned `SearchHint`, ask `MediathekGatewayActor.QueryItems`, ask `ShowActor(329324).Match(items)`, then apply `QualityExpander` and `ResultScorer` to the matched results

#### Scenario: Movie search full flow
- **WHEN** a `Search.Movie(imdbId="tt0082096", query=null)` arrives
- **THEN** the actor SHALL ask `MovieActor("tt0082096").ResolveSearch`, build a `MediathekSearchQuery` from the returned `SearchHint`, ask `MediathekGatewayActor.QueryItems`, ask `MovieActor("tt0082096").Match(items)`, then apply `QualityExpander` and `ResultScorer`

#### Scenario: SearchHint with topic builds ByTopic query
- **WHEN** the `SearchHint` has `Topic="Feuer & Flamme"` and `Channels=["WDR"]` and `MinDuration=2100`
- **THEN** the actor SHALL build `MediathekSearchQuery.ByTopic("Feuer & Flamme").FromChannel("WDR").WithDuration(min: 2100).ExcludeFuture()`

#### Scenario: SearchHint without topic builds BySearch query
- **WHEN** the `SearchHint` has `Topic=null` and `SearchTerm="New Show"`
- **THEN** the actor SHALL build `MediathekSearchQuery.BySearch("New Show").ExcludeFuture()`

### Requirement: Text search path
The `SearchRequestActor` SHALL handle text searches directly without delegating to a media actor: query MediathekGateway, apply ContentFilter, QualityExpander, and ResultScorer.

#### Scenario: Text search with query
- **WHEN** a `Search.Text(query="Tatort")` arrives
- **THEN** the actor SHALL build `MediathekSearchQuery.BySearch("Tatort").Limit(200)`, ask `MediathekGatewayActor`, apply `ContentFilter.ShouldSkip`, `QualityExpander.ExpandMany`, and `ResultScorer.ScoreAndSort`

#### Scenario: Text search without query (browse/RSS)
- **WHEN** a `Search.Text(query=null)` arrives
- **THEN** the actor SHALL build `MediathekSearchQuery.Latest().Limit(100)`, ask `MediathekGatewayActor`, apply `ContentFilter.ShouldSkip`, `QualityExpander.ExpandMany`, and `ResultScorer.ScoreAndSort`

### Requirement: Result caching
The `SearchRequestActor` SHALL cache search results for 55 minutes per entity key. Subsequent searches with the same entity key SHALL return cached results.

#### Scenario: Cache hit
- **WHEN** a `Search.Tv(tvdbId=329324)` arrives within 55 minutes of a previous identical search
- **THEN** the actor SHALL return cached results without querying MediathekGateway or the ShowActor

#### Scenario: Cache miss after expiry
- **WHEN** a `Search.Tv(tvdbId=329324)` arrives more than 55 minutes after the last search
- **THEN** the actor SHALL execute the full two-phase pipeline

### Requirement: Caller coalescing
The `SearchRequestActor` SHALL coalesce concurrent requests for the same entity key. While a pipeline is in-flight, additional callers SHALL be queued and receive the same result.

#### Scenario: Concurrent TV searches
- **WHEN** three `Search.Tv(tvdbId=329324)` messages arrive while the first is still processing
- **THEN** only one pipeline SHALL execute and all three callers SHALL receive the same result

### Requirement: Per-caller episode filtering
For TV searches, the `SearchRequestActor` SHALL filter cached results to the caller's requested season and episode before responding.

#### Scenario: Specific episode requested
- **WHEN** a caller requests `Search.Tv(tvdbId=329324, season=1, episode=3)` and cached results contain episodes S01E01 through S01E08
- **THEN** the response SHALL contain only results matching S01E03

#### Scenario: Full season requested
- **WHEN** a caller requests `Search.Tv(tvdbId=329324, season=1, episode=null)`
- **THEN** the response SHALL contain all cached results for season 1

### Requirement: SearchResponse format
The `SearchRequestActor` SHALL respond with `SearchResponse(SearchResult[] Results)` using the same `SearchResult` record as the current search actors.

#### Scenario: Response structure
- **WHEN** a search completes with 5 matched items expanded to 12 quality variants
- **THEN** the response SHALL contain 12 `SearchResult` records sorted by score descending
