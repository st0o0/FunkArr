## MODIFIED Requirements

### Requirement: RuleSet registry actor
The system SHALL provide a `RuleSetRegistryActor` registered via Akka.Hosting under the name `"ruleset-registry"` as a `ReceivePersistentActor` with `PersistenceId = "ruleset-registry"`. The actor SHALL manage community ruleset loading from disk, track local override registrations via event sourcing, and push rules to `SeriesRuleSetActor` and `MovieRuleSetActor` entities. The actor SHALL maintain a persistent catalog of all known rulesets (community and local) for catalog queries.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the `RuleSetRegistryActor` SHALL be registered in the ActorSystem and resolvable via IActorRegistry

#### Scenario: Push community rules to SeriesRuleSetActors on startup
- **WHEN** the application starts and community rulesets are loaded from disk
- **THEN** for each ruleset with a `media.tvdbId`, the actor SHALL send `ApplyCommunityRules(ruleSet)` to `SeriesRuleSetActor(tvdbId)`

#### Scenario: Push community rules to MovieRuleSetActors on startup
- **WHEN** the application starts and community rulesets with `media.type = "movie"` are loaded
- **THEN** for each such ruleset with a `media.imdbId`, the actor SHALL send `ApplyCommunityRules(ruleSet)` to `MovieRuleSetActor(imdbId)`

#### Scenario: Recovery restores catalog
- **WHEN** the actor restarts and recovers from its journal
- **THEN** the catalog SHALL contain all previously registered local overrides alongside community entries loaded from disk

### Requirement: Local override registration
The system SHALL persist a `LocalRegistered` event when a local override is saved via `SaveLocal`. The actor SHALL forward `ApplyLocalOverride` to the corresponding `SeriesRuleSetActor`/`MovieRuleSetActor` after persisting.

#### Scenario: Save local override
- **WHEN** `SaveLocal(entityKey, mediaType, ruleSet)` is received
- **THEN** the actor SHALL persist `LocalRegistered`, forward `ApplyLocalOverride` to the media actor, and reply with success

#### Scenario: Save local reply timing
- **WHEN** `SaveLocal` is processed
- **THEN** the reply SHALL be sent after the `LocalRegistered` event is persisted, not before

### Requirement: Local override removal
The system SHALL persist a `LocalRemoved` event when a local override is deleted via `RemoveLocal`.

#### Scenario: Remove local override
- **WHEN** `RemoveLocal(entityKey)` is received
- **THEN** the actor SHALL persist `LocalRemoved`, forward `RemoveLocalOverride` to the media actor, and reply with success
