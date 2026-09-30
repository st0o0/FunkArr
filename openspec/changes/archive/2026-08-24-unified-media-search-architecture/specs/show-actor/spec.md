## ADDED Requirements

### Requirement: ShowActor sharded persistent actor
The system SHALL provide a `ShowActor` as a Cluster Sharding entity keyed by `tvdbId` (string). The actor SHALL be persistent (event-sourced) with PersistenceId `"show-{tvdbId}"` and passivate after 6 hours of inactivity.

#### Scenario: Actor creation on first message
- **WHEN** a `ResolveSearch` message arrives for tvdbId "329324"
- **THEN** the shard region SHALL create a `ShowActor` entity with PersistenceId `"show-329324"`

#### Scenario: Passivation after inactivity
- **WHEN** a `ShowActor` receives no messages for 6 hours
- **THEN** it SHALL passivate and release its memory

#### Scenario: Recovery from journal
- **WHEN** a passivated `ShowActor` receives a new message
- **THEN** it SHALL recover its state from the event journal before processing

### Requirement: Show identity resolution
The `ShowActor` SHALL resolve show identity via the TVDB API on first access and cache the result. The cache SHALL expire after 24 hours. Resolution includes show name and episodes for the requested season.

#### Scenario: First resolution
- **WHEN** a `ResolveSearch` message arrives and no cached identity exists
- **THEN** the actor SHALL call `TvdbClient.GetShowAsync(tvdbId)` and `TvdbClient.GetEpisodesAsync(tvdbId, season)` and persist a `ShowResolved` event

#### Scenario: Cached resolution
- **WHEN** a `ResolveSearch` message arrives within 24 hours of the last resolution
- **THEN** the actor SHALL use the cached show name and episodes without calling the TVDB API

#### Scenario: Cache expiry
- **WHEN** a `ResolveSearch` message arrives more than 24 hours after the last resolution
- **THEN** the actor SHALL re-resolve from the TVDB API and persist a new `ShowResolved` event

#### Scenario: Additional season requested
- **WHEN** a `ResolveSearch` arrives for season 3 but only season 1 episodes are cached
- **THEN** the actor SHALL fetch season 3 episodes from TVDB and add them to the cache

### Requirement: ResolveSearch message
The `ShowActor` SHALL handle a `ResolveSearch(ShowName, Season)` message and respond with a `SearchHint` containing the topic string, channel whitelist, minimum duration, and episode list for the requested season.

#### Scenario: Known show with rules
- **WHEN** a `ResolveSearch("Feuer & Flamme", season=1)` arrives and the actor has a ruleset with topic "Feuer & Flamme", channels ["WDR"], and a duration filter > 35
- **THEN** the actor SHALL respond with `SearchHint(Topic="Feuer & Flamme", Channels=["WDR"], MinDuration=2100, Episodes=[...], SearchTerm=null)`

#### Scenario: Unknown show without rules
- **WHEN** a `ResolveSearch("New Show", season=1)` arrives and no ruleset exists
- **THEN** the actor SHALL respond with `SearchHint(Topic=null, Channels=null, MinDuration=0, Episodes=[...], SearchTerm="New Show")`

#### Scenario: Show with topic but no duration filter
- **WHEN** a ruleset exists with a topic but no duration filter in any rule
- **THEN** the `SearchHint` SHALL have `MinDuration=0`

### Requirement: Match message
The `ShowActor` SHALL handle a `Match(Season, Episode, Items[])` message by applying ruleset matching against TVDB episodes and responding with `MatchedResults`.

#### Scenario: Match with existing rules
- **WHEN** `Match(season=1, episode=null, items)` arrives and rules exist
- **THEN** the actor SHALL call `RuleSetMatchingEngine.EvaluateRules(items, rules, episodes)` and respond with matched results

#### Scenario: Match without rules triggers inline generation
- **WHEN** `Match(items)` arrives and no rules exist
- **THEN** the actor SHALL call `RuleSetGenerator.Generate(items, tvdbId, showName, episodes)` to create rules, persist a `RulesGenerated` event, apply the new rules immediately, and respond with matched results in the same request

#### Scenario: Match with empty items
- **WHEN** `Match(items)` arrives with an empty items array
- **THEN** the actor SHALL respond with empty `MatchedResults`

### Requirement: Ruleset ownership
The `ShowActor` SHALL own its ruleset as persistent state with three logical layers: community (pushed by RuleSetRegistryActor), generated (created inline), and local overrides (user-defined). The effective ruleset SHALL be the merge of all layers with priority: local > generated > community.

#### Scenario: Community rules applied
- **WHEN** the `RuleSetRegistryActor` sends `ApplyCommunityRules(ruleSet)` to the actor
- **THEN** the actor SHALL persist a `CommunityRulesApplied` event, update the community layer, and recompute the merged ruleset

#### Scenario: Generated rules created
- **WHEN** inline auto-generation produces a ruleset with confidence 0.75
- **THEN** the actor SHALL persist a `RulesGenerated` event with the rules and confidence score

#### Scenario: Local override applied
- **WHEN** an `ApplyLocalOverride(override)` message arrives
- **THEN** the actor SHALL persist a `LocalOverrideApplied` event and recompute the merged ruleset

#### Scenario: Community update preserves generated rules
- **WHEN** new community rules arrive via `ApplyCommunityRules` and generated rules already exist
- **THEN** the community layer SHALL be replaced but the generated layer SHALL be preserved

#### Scenario: Merge priority
- **WHEN** community has rule A at priority 0, generated has rule B at priority 5, and local has rule C at priority 0
- **THEN** the effective ruleset SHALL contain rules C (local wins over community at priority 0), B, sorted by priority

### Requirement: Match quality tracking
The `ShowActor` SHALL track match quality statistics as part of its persistent state. Statistics SHALL include match rate, per-rule hit counts, and unmatched item tracking with a 7-day rolling window.

#### Scenario: Record match results
- **WHEN** a `Match` operation completes with 15 matched and 5 unmatched items
- **THEN** the actor SHALL persist a `MatchQualityRecorded` event with the statistics

#### Scenario: Query match quality
- **WHEN** a `GetMatchQuality` message arrives
- **THEN** the actor SHALL respond with current match rate, per-rule hit counts, and recent unmatched items

#### Scenario: Rolling window eviction
- **WHEN** match records older than 7 days exist
- **THEN** they SHALL be excluded from statistics calculations

### Requirement: Persistence events
The `ShowActor` SHALL use the following event types for persistence: `ShowResolved`, `CommunityRulesApplied`, `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`. The actor SHALL snapshot every 500 events.

#### Scenario: Snapshot creation
- **WHEN** 500 events have been persisted since the last snapshot
- **THEN** the actor SHALL create a snapshot containing the full current state

#### Scenario: Recovery from snapshot
- **WHEN** a snapshot exists and 50 events follow it
- **THEN** recovery SHALL load the snapshot and replay only the 50 subsequent events
