## ADDED Requirements

### Requirement: MovieActor sharded persistent actor
The system SHALL provide a `MovieActor` as a Cluster Sharding entity keyed by `imdbId` (string). The actor SHALL be persistent (event-sourced) with PersistenceId `"movie-{imdbId}"` and passivate after 6 hours of inactivity.

#### Scenario: Actor creation on first message
- **WHEN** a `ResolveSearch` message arrives for imdbId "tt0082096"
- **THEN** the shard region SHALL create a `MovieActor` entity with PersistenceId `"movie-tt0082096"`

#### Scenario: Passivation after inactivity
- **WHEN** a `MovieActor` receives no messages for 6 hours
- **THEN** it SHALL passivate and release its memory

#### Scenario: Recovery from journal
- **WHEN** a passivated `MovieActor` receives a new message
- **THEN** it SHALL recover its state from the event journal before processing

### Requirement: Movie identity resolution
The `MovieActor` SHALL resolve movie identity via the TMDB API on first access and cache the result. The cache SHALL expire after 24 hours. Resolution includes title, original title, release year, and runtime.

#### Scenario: Resolution by IMDB ID
- **WHEN** a `ResolveSearch` message arrives with imdbId "tt0082096" and no cached identity exists
- **THEN** the actor SHALL call `TmdbClient.FindByImdbIdAsync("tt0082096")` and persist a `MovieResolved` event

#### Scenario: Resolution by search term
- **WHEN** a `ResolveSearch` message arrives with imdbId null and searchTerm "Das Boot"
- **THEN** the actor SHALL call `TmdbClient.SearchMovieAsync("Das Boot")` and persist a `MovieResolved` event

#### Scenario: Cached resolution
- **WHEN** a `ResolveSearch` message arrives within 24 hours of the last resolution
- **THEN** the actor SHALL use the cached movie info without calling the TMDB API

### Requirement: ResolveSearch message
The `MovieActor` SHALL handle a `ResolveSearch(ImdbId, SearchTerm)` message and respond with a `SearchHint` containing the movie title, original title, minimum duration derived from known runtime, and search term for Mediathek query.

#### Scenario: Known movie with rules
- **WHEN** a `ResolveSearch` arrives and the actor has a ruleset with topic "Das Boot"
- **THEN** the actor SHALL respond with `SearchHint(Topic="Das Boot", MinDuration=6300, SearchTerm=null, OriginalTitle="Das Boot")`

#### Scenario: Known movie without rules
- **WHEN** a `ResolveSearch` arrives and no ruleset exists but TMDB resolution succeeded
- **THEN** the actor SHALL respond with `SearchHint(Topic=null, MinDuration=6300, SearchTerm="Das Boot", OriginalTitle="Das Boot")`

#### Scenario: MinDuration derived from runtime
- **WHEN** the TMDB runtime is 149 minutes
- **THEN** the `SearchHint.MinDuration` SHALL be 70% of runtime in seconds (approximately 6258 seconds)

### Requirement: Match message
The `MovieActor` SHALL handle a `Match(Items[])` message by applying ruleset matching against movie identity and responding with `MatchedResults`.

#### Scenario: Match with existing rules
- **WHEN** `Match(items)` arrives and rules exist
- **THEN** the actor SHALL call `RuleSetMatchingEngine.EvaluateMovieRules(items, rules, movieInfo)` and respond with matched results

#### Scenario: Match without rules triggers inline generation
- **WHEN** `Match(items)` arrives and no rules exist
- **THEN** the actor SHALL call `RuleSetGenerator.GenerateForMovie(items, movieInfo)` to create rules, persist a `RulesGenerated` event, apply the new rules immediately, and respond with matched results

#### Scenario: Fallback to original title
- **WHEN** matching with the primary title yields zero results and an original title differs from the primary title
- **THEN** the actor SHALL retry matching using the original title before responding

### Requirement: Ruleset ownership
The `MovieActor` SHALL own its ruleset as persistent state with the same three-layer merge logic as `ShowActor`: community > generated > local, with local overrides winning.

#### Scenario: Community rules applied
- **WHEN** the `RuleSetRegistryActor` sends `ApplyCommunityRules(ruleSet)` to the actor
- **THEN** the actor SHALL persist a `CommunityRulesApplied` event and recompute the merged ruleset

#### Scenario: Generated rules created inline
- **WHEN** inline auto-generation produces a movie ruleset
- **THEN** the actor SHALL persist a `RulesGenerated` event

### Requirement: Match quality tracking
The `MovieActor` SHALL track match quality statistics identically to `ShowActor`, with a 7-day rolling window.

#### Scenario: Record match results
- **WHEN** a `Match` operation completes
- **THEN** the actor SHALL persist a `MatchQualityRecorded` event with match/miss statistics

### Requirement: Persistence events
The `MovieActor` SHALL use event types: `MovieResolved`, `CommunityRulesApplied`, `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`. Snapshots every 500 events.

#### Scenario: Snapshot creation
- **WHEN** 500 events have been persisted since the last snapshot
- **THEN** the actor SHALL create a snapshot containing the full current state
