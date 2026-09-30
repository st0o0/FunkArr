## ADDED Requirements

### Requirement: MediaRuleSetActor abstract base class
The system SHALL provide an abstract `MediaRuleSetActor` extending `ReceivePersistentActor` that holds the shared shape for all media-kind entity actors. The base class SHALL manage three rule-set layer slots (community, generated, local), persistence, the match request/response pipeline, and passivation. The base class SHALL NOT reference any provider, HTTP client, or I/O dependency. The base class SHALL NOT exceed 200 lines or 8 message handlers.

#### Scenario: Base class has no provider dependencies
- **WHEN** inspecting `MediaRuleSetActor` constructor parameters and fields
- **THEN** no parameter or field SHALL reference a `*Provider`, `*Client`, `HttpClient`, or any I/O type

#### Scenario: Base class line count
- **WHEN** measuring `MediaRuleSetActor.cs`
- **THEN** the file SHALL NOT exceed 200 lines

#### Scenario: Base class handler count
- **WHEN** counting message handler registrations in `MediaRuleSetActor`
- **THEN** there SHALL be at most 8 handlers

### Requirement: Four-hook contract
The `MediaRuleSetActor` SHALL declare exactly four `protected abstract` hooks that subclasses implement. The hook contract is closed — no fifth hook SHALL be added.

#### Scenario: ResolveMetadata hook
- **WHEN** the base class needs metadata for a match operation
- **THEN** it SHALL call `ResolveMetadata` which the subclass implements to ask the appropriate gateway actor

#### Scenario: BuildSearchHint hook
- **WHEN** a `ResolveSearch` message arrives
- **THEN** the base class SHALL call `BuildSearchHint` which the subclass implements to construct a `SearchHint` from resolved metadata and effective rules

#### Scenario: SelectStrategies hook
- **WHEN** the match pipeline needs to evaluate items against rules
- **THEN** the base class SHALL call `SelectStrategies` which the subclass implements to dispatch to the correct `RuleSetMatchingEngine` method (TV or movie)

#### Scenario: ComputeCoverage hook
- **WHEN** a coverage query arrives
- **THEN** the base class SHALL call `ComputeCoverage` which the subclass implements to return episode coverage (series) or matched flag (movie)

### Requirement: Persistence identity from entity key
The `MediaRuleSetActor` SHALL accept an `entityKey` string in its constructor and set `PersistenceId = $"ruleset-{entityKey}"`. The subclass provides the formatted key (e.g., `$"series-{tvdbId}"` or `$"movie-{imdbId}"`).

#### Scenario: Series persistence ID
- **WHEN** a `SeriesRuleSetActor` is created for tvdbId 83214
- **THEN** `PersistenceId` SHALL be `"ruleset-series-83214"`

#### Scenario: Movie persistence ID
- **WHEN** a `MovieRuleSetActor` is created for imdbId "tt0082096"
- **THEN** `PersistenceId` SHALL be `"ruleset-movie-tt0082096"`

### Requirement: Three-layer ruleset management
The `MediaRuleSetActor` SHALL manage three nullable `RuleSetFile` slots: `CommunityRuleSet`, `GeneratedRuleSet`, `LocalOverrideRuleSet`. The effective ruleset SHALL be computed as `Local ?? Generated ?? Community`. Community rules SHALL NOT be persisted.

#### Scenario: Community rules applied transiently
- **WHEN** `ApplyCommunityRules(ruleSetFile)` is received
- **THEN** the actor SHALL update the community slot in RAM, recompute effective rules, and NOT persist any event

#### Scenario: Local override applied with persistence
- **WHEN** `ApplyLocalOverride(ruleSetFile)` is received
- **THEN** the actor SHALL persist a `LocalOverrideChanged` event with the ruleset and recompute effective rules

#### Scenario: Local override removed with persistence
- **WHEN** `RemoveLocalOverride` is received
- **THEN** the actor SHALL persist a `LocalOverrideChanged` event with null payload and recompute effective rules

#### Scenario: Generated rules persisted
- **WHEN** inline auto-generation produces a ruleset
- **THEN** the actor SHALL persist a `RulesGenerated` event and recompute effective rules

#### Scenario: Recovery without community rules
- **WHEN** the actor recovers from journal
- **THEN** generated and local rules SHALL be restored, but community SHALL be null until the registry pushes them

### Requirement: Entity persistence events
The `MediaRuleSetActor` SHALL persist exactly two event types: `RulesGenerated(RuleSetFile, Confidence, At)` and `LocalOverrideChanged(RuleSetFile?, At)`. No other event types SHALL be persisted by the entity. The actor SHALL NOT use snapshots.

#### Scenario: RulesGenerated event
- **WHEN** auto-generation produces rules with confidence 0.85
- **THEN** the actor SHALL persist `RulesGenerated` with the ruleset, confidence, and timestamp

#### Scenario: LocalOverrideChanged with ruleset
- **WHEN** a local override is applied
- **THEN** the actor SHALL persist `LocalOverrideChanged` with the `RuleSetFile` payload

#### Scenario: LocalOverrideChanged with null (cleared)
- **WHEN** a local override is removed
- **THEN** the actor SHALL persist `LocalOverrideChanged` with null payload

#### Scenario: No analytics events persisted
- **WHEN** a match completes
- **THEN** the actor SHALL NOT persist `MatchQualityRecorded`, `EpisodeMatched`, or `MatchRateSnapshotRecorded` events

### Requirement: Match pipeline
The `MediaRuleSetActor` SHALL handle `Match` messages with the following pipeline: (1) resolve metadata via hook, (2) if no effective rules and items exist, auto-generate via `RuleSetGenerator`, (3) evaluate rules via the `SelectStrategies` hook, (4) emit `MatchRunRecorded` to `MatchStatsActor` via Tell, (5) reply with `MatchedResults`. When no rules exist after auto-generation attempt, the fallback path SHALL apply `ContentFilter` and NOT tell `MatchStatsActor`.

#### Scenario: Match with rules
- **WHEN** `Match(items)` arrives and effective rules exist
- **THEN** the actor SHALL resolve metadata, evaluate rules via hook, Tell `MatchStatsActor` with `MatchRunRecorded`, and reply with results

#### Scenario: Match without rules triggers generation
- **WHEN** `Match(items)` arrives and no rules exist
- **THEN** the actor SHALL auto-generate rules, persist `RulesGenerated`, evaluate, tell stats, and reply

#### Scenario: Fallback when generation fails
- **WHEN** generation produces nothing
- **THEN** the actor SHALL apply `ContentFilter`, NOT tell `MatchStatsActor`, and reply with fallback results

#### Scenario: Empty items
- **WHEN** `Match(items)` arrives with empty array
- **THEN** the actor SHALL reply with empty `MatchedResults`

### Requirement: Passivation
The `MediaRuleSetActor` SHALL passivate after 6 hours of inactivity via Akka Cluster Sharding `Passivate`.

#### Scenario: Passivation timeout
- **WHEN** the actor receives no messages for 6 hours
- **THEN** it SHALL passivate and release memory

### Requirement: GetRuleSet message
The `MediaRuleSetActor` SHALL handle `GetRuleSet` and respond with `RuleSetResponse` containing the effective ruleset and source layer info. Match quality stats SHALL be queried from `MatchStatsActor` (not held locally).

#### Scenario: Rules exist
- **WHEN** `GetRuleSet` arrives and effective rules exist
- **THEN** the actor SHALL respond with the ruleset and source designation

#### Scenario: No rules
- **WHEN** `GetRuleSet` arrives and no rules exist
- **THEN** the actor SHALL respond with null

### Requirement: No metadata cache in entity
The `MediaRuleSetActor` SHALL NOT cache metadata (show name, episodes, movie title) in its state. Metadata SHALL be resolved from the gateway on each request via the `ResolveMetadata` hook.

#### Scenario: No metadata fields in state
- **WHEN** inspecting `MediaRuleSetActorState`
- **THEN** there SHALL be no fields for show name, episodes, movie title, or any gateway-derived data

### Requirement: SeriesRuleSetActor subclass
The system SHALL provide a `SeriesRuleSetActor` extending `MediaRuleSetActor` with typed id `int` (tvdbId). Constructor: `SeriesRuleSetActor(int id) : MediaRuleSetActor($"series-{id}")`. The subclass SHALL implement four hooks using `TvdbGatewayActor` and SHALL NOT exceed ~50 lines.

#### Scenario: Series hook implementations
- **WHEN** `ResolveMetadata` is called
- **THEN** it SHALL ask `TvdbGatewayActor` for show info and episodes

#### Scenario: Series strategies
- **WHEN** `SelectStrategies` is called
- **THEN** it SHALL dispatch to `RuleSetMatchingEngine.EvaluateRulesWithTraces`

#### Scenario: Series coverage
- **WHEN** `ComputeCoverage` is called
- **THEN** it SHALL return episode-level coverage data

#### Scenario: ResolveSearch for series
- **WHEN** `ResolveSearch(ShowName, Season)` arrives
- **THEN** the actor SHALL resolve TVDB data and respond with `SearchHint` including topic, channels, minDuration, and episodes

### Requirement: MovieRuleSetActor subclass
The system SHALL provide a `MovieRuleSetActor` extending `MediaRuleSetActor` with typed id `string` (imdbId). Constructor: `MovieRuleSetActor(string id) : MediaRuleSetActor($"movie-{id}")`. The subclass SHALL implement four hooks using `TmdbGatewayActor` and SHALL NOT exceed ~50 lines.

#### Scenario: Movie hook implementations
- **WHEN** `ResolveMetadata` is called
- **THEN** it SHALL ask `TmdbGatewayActor` for movie info

#### Scenario: Movie strategies
- **WHEN** `SelectStrategies` is called
- **THEN** it SHALL dispatch to `RuleSetMatchingEngine.EvaluateMovieRulesWithTraces`

#### Scenario: Movie coverage
- **WHEN** `ComputeCoverage` is called
- **THEN** it SHALL return movie-level matched/unmatched flag

#### Scenario: Movie original title fallback
- **WHEN** primary title matching yields zero results and original title differs
- **THEN** the actor SHALL retry matching with the original title

#### Scenario: ResolveSearch for movie
- **WHEN** `ResolveSearch(ImdbId, SearchTerm)` arrives
- **THEN** the actor SHALL resolve TMDB data and respond with `SearchHint` including topic, minDuration, searchTerm, originalTitle
