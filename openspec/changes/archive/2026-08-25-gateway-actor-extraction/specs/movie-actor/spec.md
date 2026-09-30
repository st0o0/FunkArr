## MODIFIED Requirements

### Requirement: Movie identity resolution
The `MovieActor` SHALL resolve movie identity by asking the `TmdbGatewayActor` via the Ask pattern. The response SHALL be cached transiently in RAM (not persisted). No `MovieResolved` event SHALL be persisted. The actor SHALL NOT have a direct dependency on `TmdbClient`.

#### Scenario: Resolution by IMDB ID via gateway
- **WHEN** a `ResolveSearch` message arrives with imdbId "tt0082096" and no transient movie info exists
- **THEN** the actor SHALL send `GetMovieInfo("tt0082096")` to `TmdbGatewayActor` and cache the response in RAM fields

#### Scenario: Resolution by search term via gateway
- **WHEN** a `ResolveSearch` message arrives with imdbId null and searchTerm "Das Boot"
- **THEN** the actor SHALL send `SearchMovie("Das Boot")` to `TmdbGatewayActor` and cache the response

#### Scenario: Cached resolution (transient)
- **WHEN** a `ResolveSearch` message arrives and transient movie info exists
- **THEN** the actor SHALL use the transient cache without asking the gateway

#### Scenario: Re-activation after passivation
- **WHEN** a passivated MovieActor is re-activated by a new message
- **THEN** it SHALL ask the gateway for movie info (gateway responds from its event-sourced cache)

### Requirement: Ruleset ownership
The `MovieActor` SHALL own its ruleset with the same three-layer merge logic as `ShowActor`: community (transient, pushed by registry), generated (persisted), local overrides (persisted). Community rules SHALL NOT be persisted — they are held in RAM only and re-pushed by the registry on startup.

#### Scenario: Community rules applied (transient)
- **WHEN** the `RuleSetRegistryActor` sends `ApplyCommunityRules(ruleSet)` to the actor
- **THEN** the actor SHALL update the community layer in RAM and recompute the merged ruleset — no event SHALL be persisted

#### Scenario: Generated rules created inline (persisted)
- **WHEN** inline auto-generation produces a movie ruleset
- **THEN** the actor SHALL persist a `RulesGenerated` event

#### Scenario: Recovery without community rules
- **WHEN** the MovieActor recovers from its journal
- **THEN** it SHALL have generated and local rules from the journal, but community rules SHALL be null until the registry pushes them

### Requirement: Persistence events
The `MovieActor` SHALL use event types: `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`, `LocalOverrideRemoved`. The actor SHALL NOT persist `MovieResolved` or `CommunityRulesApplied` events. The actor SHALL NOT use snapshots.

#### Scenario: No MovieResolved events
- **WHEN** the actor resolves movie identity via the gateway
- **THEN** no `MovieResolved` event SHALL be persisted

#### Scenario: No CommunityRulesApplied events
- **WHEN** community rules are pushed by the registry
- **THEN** no `CommunityRulesApplied` event SHALL be persisted

#### Scenario: No snapshot logic
- **WHEN** any number of events have been persisted
- **THEN** no snapshot SHALL be created

### Requirement: Constructor dependencies
The `MovieActor` SHALL be constructed with `IReadOnlyActorRegistry` only. It SHALL NOT depend on `TmdbClient`. The gateway actor SHALL be resolved from the registry at runtime.

#### Scenario: No TmdbClient injection
- **WHEN** the MovieActor is instantiated
- **THEN** its constructor SHALL NOT accept a `TmdbClient` parameter

#### Scenario: Gateway resolved from registry
- **WHEN** the MovieActor needs TMDB data
- **THEN** it SHALL resolve `TmdbGatewayActor` from `IReadOnlyActorRegistry`

## REMOVED Requirements

### Requirement: Snapshot support (from "Persistence events" in existing spec)
**Reason**: Event volume in MovieActor is very low (only generated rules, local overrides, and match quality). Snapshot-based recovery provides no measurable benefit.
**Migration**: Delete `MovieActorSnapshot` record, `ToSnapshot`/`FromSnapshot` methods, `SaveSnapshotSuccess`/`SaveSnapshotFailure` handlers, and the `SnapshotInterval` constant.
