## MODIFIED Requirements

### Requirement: RuleSet registry actor
The system SHALL provide a `RuleSetRegistryActor` (renamed back from `RuleSetActor`) registered via Akka.Hosting under the name `"ruleset-registry"` as a singleton actor. The actor SHALL manage community ruleset loading from disk and GitHub Releases, and push community rules to `ShowActor` and `MovieActor` entities. The actor SHALL NOT maintain an in-memory query index, SHALL NOT handle topic/alias/TVDB lookups, SHALL NOT handle auto-generation triggers, and SHALL NOT handle CRUD operations for local overrides. These responsibilities move to `ShowActor` and `MovieActor`.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the `RuleSetRegistryActor` SHALL be registered in the ActorSystem and resolvable via IActorRegistry

#### Scenario: Push community rules to ShowActors on startup
- **WHEN** the application starts and community rulesets are loaded from disk
- **THEN** for each ruleset with a `media.tvdbId`, the actor SHALL send `ApplyCommunityRules(ruleSet)` to `ShowActor(tvdbId)`

#### Scenario: Push community rules to MovieActors on startup
- **WHEN** the application starts and community rulesets with `media.type = "movie"` are loaded
- **THEN** for each such ruleset with a `media.imdbId`, the actor SHALL send `ApplyCommunityRules(ruleSet)` to `MovieActor(imdbId)`

### Requirement: Community refresh
The system SHALL periodically refresh community rulesets by querying the GitHub Releases API for the configured repository, downloading the ZIP asset, extracting it atomically, and pushing updated rules to the affected ShowActors/MovieActors. The refresh interval SHALL default to 60 minutes.

#### Scenario: GitHub release refresh with diff
- **WHEN** the refresh timer fires and a newer version is available
- **THEN** the system SHALL download the ZIP, extract to `community/`, diff against the previous version, and push only changed rulesets to the affected ShowActors/MovieActors

#### Scenario: Refresh failure
- **WHEN** the GitHub API is unreachable during a refresh attempt
- **THEN** the system SHALL log a warning and retain the existing community files

### Requirement: Startup loading
The system SHALL load all community ruleset JSON files from `data/community/rulesets/` at startup. Generated and local rulesets are no longer loaded by this actor — they are owned by ShowActor/MovieActor as persistent state.

#### Scenario: Community files loaded at startup
- **WHEN** the application starts with 150 files in `data/community/rulesets/`
- **THEN** the actor SHALL load all files and push them to the corresponding ShowActors/MovieActors

#### Scenario: Missing directory
- **WHEN** the `data/community/rulesets/` directory does not exist at startup
- **THEN** the system SHALL create it and continue with no community rules to push

#### Scenario: Malformed JSON file
- **WHEN** a community ruleset file contains invalid JSON
- **THEN** the system SHALL log a warning with the filename and skip that file

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

## REMOVED Requirements

### Requirement: Topic alias indexing
**Reason**: Topic/alias indexing responsibility moves to ShowActor/MovieActor which own their own rulesets as persistent state.
**Migration**: ShowActor receives community rules via `ApplyCommunityRules` and maintains its own topic identity.

### Requirement: Three-layer resolution with merge support
**Reason**: Three-layer merge logic moves to ShowActor/MovieActor where all layers are owned as persistent state.
**Migration**: ShowActor/MovieActor implement the same community > generated > local merge priority internally.

### Requirement: Query by topic
**Reason**: Topic-based lookup is no longer needed. Callers address ShowActor/MovieActor directly by tvdbId/imdbId.
**Migration**: `SearchRequestActor` sends `ResolveSearch` to `ShowActor(tvdbId)` or `MovieActor(imdbId)` directly.

### Requirement: Auto-generation trigger
**Reason**: Auto-generation moves inline into ShowActor/MovieActor where items and episodes are both available.
**Migration**: ShowActor/MovieActor call `RuleSetGenerator` directly during `Match` when no rules exist.

### Requirement: List all rulesets message
**Reason**: Centralized listing moves to a future API that queries ShowActors/MovieActors or the RuleSetRegistryActor's community catalog.
**Migration**: The RuleSetRegistryActor can still list community rulesets it loaded from disk.

### Requirement: Get single ruleset message
**Reason**: Replaced by direct communication with ShowActor/MovieActor.
**Migration**: Query the specific ShowActor(tvdbId) or MovieActor(imdbId) for its ruleset.

### Requirement: Save local override message
**Reason**: Local overrides are now persistent events in ShowActor/MovieActor.
**Migration**: Send `ApplyLocalOverride` directly to the ShowActor/MovieActor.

### Requirement: Delete local override message
**Reason**: Local overrides are now persistent events in ShowActor/MovieActor.
**Migration**: Send a removal message directly to the ShowActor/MovieActor.

### Requirement: Test rules message
**Reason**: Testing rules against Mediathek items can be done by sending items to ShowActor.Match() with trace output.
**Migration**: Use ShowActor/MovieActor Match with trace mode enabled.
