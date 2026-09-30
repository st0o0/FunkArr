## MODIFIED Requirements

### Requirement: Show identity resolution
The `ShowActor` SHALL resolve show identity by asking the `TvdbGatewayActor` via the Ask pattern. The response SHALL be cached transiently in RAM (not persisted). No `ShowResolved` event SHALL be persisted. The actor SHALL NOT have a direct dependency on `TvdbClient`.

#### Scenario: First resolution via gateway
- **WHEN** a `ResolveSearch` message arrives and no transient show info exists
- **THEN** the actor SHALL send `ResolveShow(tvdbId, season)` to `TvdbGatewayActor` and cache the response in RAM fields

#### Scenario: Cached resolution (transient)
- **WHEN** a `ResolveSearch` message arrives and transient show info exists from a recent gateway response
- **THEN** the actor SHALL use the transient cache without asking the gateway

#### Scenario: Cache expiry delegated to gateway
- **WHEN** the ShowActor asks the gateway for show info
- **THEN** the gateway SHALL handle TTL expiry internally — the ShowActor does not track TTL for TVDB data

#### Scenario: Additional season requested
- **WHEN** a `ResolveSearch` arrives for season 3 but only season 1 episodes are in transient cache
- **THEN** the actor SHALL ask the gateway for season 3 via `ResolveShow(tvdbId, season: 3)`

#### Scenario: Re-activation after passivation
- **WHEN** a passivated ShowActor is re-activated by a new message
- **THEN** it SHALL ask the gateway for show info (gateway responds from its event-sourced cache)

### Requirement: Ruleset ownership
The `ShowActor` SHALL own its ruleset with three logical layers: community (transient, pushed by RuleSetRegistryActor), generated (persisted), and local overrides (persisted). The effective ruleset SHALL be the merge of all layers with priority: local > generated > community. Community rules SHALL NOT be persisted — they are held in RAM only and re-pushed by the registry on startup.

#### Scenario: Community rules applied (transient)
- **WHEN** the `RuleSetRegistryActor` sends `ApplyCommunityRules(ruleSet)` to the actor
- **THEN** the actor SHALL update the community layer in RAM and recompute the merged ruleset — no event SHALL be persisted

#### Scenario: Generated rules created (persisted)
- **WHEN** inline auto-generation produces a ruleset with confidence 0.75
- **THEN** the actor SHALL persist a `RulesGenerated` event with the rules and confidence score

#### Scenario: Local override applied (persisted)
- **WHEN** an `ApplyLocalOverride(override)` message arrives
- **THEN** the actor SHALL persist a `LocalOverrideApplied` event and recompute the merged ruleset

#### Scenario: Community update preserves generated rules
- **WHEN** new community rules arrive via `ApplyCommunityRules` and generated rules already exist
- **THEN** the community layer SHALL be replaced but the generated layer SHALL be preserved

#### Scenario: Recovery without community rules
- **WHEN** the ShowActor recovers from its journal
- **THEN** it SHALL have generated and local rules from the journal, but community rules SHALL be null until the registry pushes them

#### Scenario: Merge priority
- **WHEN** community has rule A at priority 0, generated has rule B at priority 5, and local has rule C at priority 0
- **THEN** the effective ruleset SHALL contain rules C (local wins over community at priority 0), B, sorted by priority

### Requirement: Persistence events
The `ShowActor` SHALL use the following event types for persistence: `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`, `LocalOverrideRemoved`. The actor SHALL NOT persist `ShowResolved` or `CommunityRulesApplied` events. The actor SHALL NOT use snapshots.

#### Scenario: No ShowResolved events
- **WHEN** the actor resolves show identity via the gateway
- **THEN** no `ShowResolved` event SHALL be persisted

#### Scenario: No CommunityRulesApplied events
- **WHEN** community rules are pushed by the registry
- **THEN** no `CommunityRulesApplied` event SHALL be persisted

#### Scenario: No snapshot logic
- **WHEN** any number of events have been persisted
- **THEN** no snapshot SHALL be created (removed — event volume is low)

#### Scenario: Recovery replays only matching events
- **WHEN** the actor recovers
- **THEN** it SHALL replay `RulesGenerated`, `LocalOverrideApplied`, `LocalOverrideRemoved`, and `MatchQualityRecorded` events only

### Requirement: Constructor dependencies
The `ShowActor` SHALL be constructed with `IReadOnlyActorRegistry` only. It SHALL NOT depend on `TvdbClient`. The gateway actor SHALL be resolved from the registry at runtime.

#### Scenario: No TvdbClient injection
- **WHEN** the ShowActor is instantiated
- **THEN** its constructor SHALL NOT accept a `TvdbClient` parameter

#### Scenario: Gateway resolved from registry
- **WHEN** the ShowActor needs TVDB data
- **THEN** it SHALL resolve `TvdbGatewayActor` from `IReadOnlyActorRegistry`

## REMOVED Requirements

### Requirement: Snapshot support (from "Persistence events" in existing spec)
**Reason**: Event volume in ShowActor is low enough (only generated rules, local overrides, and match quality) that snapshot-based recovery provides no measurable benefit. Removing simplifies the actor significantly.
**Migration**: Delete `ShowActorSnapshot` record, `ToSnapshot`/`FromSnapshot` methods, `SaveSnapshotSuccess`/`SaveSnapshotFailure` handlers, and the `SnapshotInterval` constant.
