## MODIFIED Requirements

### Requirement: RuleSet registry actor
The system SHALL provide a `RuleSetRegistryActor` registered via Akka.Hosting under the name `"ruleset-registry"` as a `ReceivePersistentActor` with `PersistenceId = "ruleset-registry"`. The actor SHALL manage community ruleset loading from disk, track local override registrations via event sourcing, and push rules to `ShowActor` and `MovieActor` entities. The actor SHALL maintain a persistent catalog of all known rulesets (community and local) for catalog queries.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the `RuleSetRegistryActor` SHALL be registered in the ActorSystem and resolvable via IActorRegistry

#### Scenario: Push community rules to ShowActors on startup
- **WHEN** the application starts and community rulesets are loaded from disk
- **THEN** for each ruleset with a `media.tvdbId`, the actor SHALL send `ApplyCommunityRules(ruleSet)` to `ShowActor(tvdbId)`

#### Scenario: Push community rules to MovieActors on startup
- **WHEN** the application starts and community rulesets with `media.type = "movie"` are loaded
- **THEN** for each such ruleset with a `media.imdbId`, the actor SHALL send `ApplyCommunityRules(ruleSet)` to `MovieActor(imdbId)`

#### Scenario: Recovery restores catalog
- **WHEN** the actor restarts and recovers from its journal
- **THEN** the catalog SHALL contain all previously registered local overrides alongside community entries loaded from disk

### Requirement: Startup loading
The system SHALL load all community ruleset JSON files from `data/community/rulesets/` at startup. After recovery, community entries in the catalog SHALL be refreshed from disk (disk is the source-of-truth for community content).

#### Scenario: Community files loaded at startup
- **WHEN** the application starts with 150 files in `data/community/rulesets/`
- **THEN** the actor SHALL load all files and push them to the corresponding ShowActors/MovieActors

#### Scenario: Missing directory
- **WHEN** the `data/community/rulesets/` directory does not exist at startup
- **THEN** the system SHALL create it and continue with no community rules to push

#### Scenario: Malformed JSON file
- **WHEN** a community ruleset file contains invalid JSON
- **THEN** the system SHALL log a warning with the filename and skip that file

### Requirement: Community refresh
The system SHALL periodically refresh community rulesets by querying the GitHub Releases API for the configured repository, downloading the ZIP asset, extracting it atomically, and pushing updated rules to the affected ShowActors/MovieActors. The refresh interval SHALL default to 60 minutes.

#### Scenario: GitHub release refresh with diff
- **WHEN** the refresh timer fires and a newer version is available
- **THEN** the system SHALL download the ZIP, extract to `community/`, diff against the previous version, and push only changed rulesets to the affected ShowActors/MovieActors

#### Scenario: Refresh failure
- **WHEN** the GitHub API is unreachable during a refresh attempt
- **THEN** the system SHALL log a warning and retain the existing community files

### Requirement: Catalog query
The system SHALL respond to `GetCatalog` messages with the full catalog including community entries, local overrides, and their source designation. When a local override exists for the same entity key as a community ruleset, the catalog entry SHALL show `Source = "local"` and the community entry SHALL be suppressed.

#### Scenario: Catalog with community only
- **WHEN** `GetCatalog` is received and only community rulesets exist
- **THEN** all entries SHALL have `Source = "community"`

#### Scenario: Catalog with local override
- **WHEN** a local override is registered for TVDB ID 83214 and a community ruleset also exists for 83214
- **THEN** the catalog SHALL contain one entry for 83214 with `Source = "local"`, not both

#### Scenario: Catalog with local-only entry
- **WHEN** a local override is registered for TVDB ID 99999 with no community equivalent
- **THEN** the catalog SHALL contain the entry with `Source = "local"`

### Requirement: Local override registration
The system SHALL persist a `LocalRegistered` event when a local override is saved via `SaveLocal`. The actor SHALL forward `ApplyLocalOverride` to the corresponding ShowActor/MovieActor after persisting.

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

### Requirement: Snapshot support
The actor SHALL save a snapshot every 100 persisted events containing the full catalog state.

#### Scenario: Snapshot taken
- **WHEN** 100 events have been persisted since the last snapshot
- **THEN** the actor SHALL save a snapshot

#### Scenario: Recovery from snapshot
- **WHEN** the actor recovers and a snapshot exists
- **THEN** it SHALL restore catalog state from the snapshot and replay only events after the snapshot sequence number

### Requirement: Configurable source
The community source SHALL be configurable via `FunkArr__RuleSet__Repository` (default `"st0o0/funkarr"`) and `FunkArr__RuleSet__Version` (default `"latest"`).

#### Scenario: Defaults
- **WHEN** no `RuleSetRepository` or `RuleSetVersion` is configured
- **THEN** the system SHALL query `st0o0/funkarr` for the latest community-rulesets release

#### Scenario: Pinned version
- **WHEN** `RuleSetVersion` is set to `"1.0.0"`
- **THEN** the system SHALL fetch the `community-rulesets-v1.0.0` release specifically

#### Scenario: Custom repository
- **WHEN** `RuleSetRepository` is set to `"myorg/my-rulesets"`
- **THEN** the system SHALL query that repository's releases
