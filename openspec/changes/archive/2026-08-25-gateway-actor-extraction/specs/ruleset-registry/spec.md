## MODIFIED Requirements

### Requirement: Community refresh
The system SHALL periodically refresh community rulesets with hash-based change detection. On each refresh cycle (disk reload or GitHub release update), the actor SHALL compute the SHA256 hash of each JSON file's content, compare against the last known hash per entity key, and only persist events and push rules for files that actually changed. Files that no longer exist on disk SHALL trigger a `RuleSetRemoved` event.

#### Scenario: Initial load with hash tracking
- **WHEN** the application starts with 60 files in `data/community/rulesets/`
- **THEN** the actor SHALL compute SHA256 hash for each file, persist a `RuleSetLoaded` event per file (containing the hash and full RuleSetFile content), and push each to the corresponding ShowActor/MovieActor

#### Scenario: Refresh with no changes
- **WHEN** the refresh timer fires and all file hashes match the last known hashes
- **THEN** no events SHALL be persisted and no push messages SHALL be sent

#### Scenario: Refresh with one changed file
- **WHEN** the refresh timer fires and only `tatort.json` has a different hash than the stored one
- **THEN** the actor SHALL persist one `RuleSetUpdated` event for Tatort and push the updated rules to `ShowActor("83214")` only

#### Scenario: Refresh with a new file added
- **WHEN** the refresh timer fires and a new file `new-show.json` exists that has no stored hash
- **THEN** the actor SHALL persist a `RuleSetLoaded` event for the new show and push rules to the corresponding media actor

#### Scenario: Refresh with a file removed
- **WHEN** the refresh timer fires and `old-show.json` no longer exists but a hash is stored for it
- **THEN** the actor SHALL persist a `RuleSetRemoved` event for that entity key

#### Scenario: GitHub release refresh with diff
- **WHEN** a newer GitHub release is downloaded and extracted to disk
- **THEN** the hash-based diff SHALL detect only the files that changed in the new release

### Requirement: Startup loading
The system SHALL load all community ruleset JSON files from disk at startup. On recovery, the actor SHALL replay its journal to reconstruct the hash map and content for all rulesets, then compare against current disk state to detect any changes since last run.

#### Scenario: Recovery with unchanged files
- **WHEN** the actor recovers from journal and disk files have not changed since last run
- **THEN** the hash comparison SHALL detect no changes and no new events SHALL be persisted

#### Scenario: Recovery with files changed while stopped
- **WHEN** the actor recovers and some disk files have been updated while the service was stopped
- **THEN** the actor SHALL persist `RuleSetUpdated` events for the changed files and push them to media actors

#### Scenario: Community files loaded at startup
- **WHEN** the application starts with 150 files in `data/community/rulesets/`
- **THEN** the actor SHALL load all files and push them to the corresponding ShowActors/MovieActors

#### Scenario: Malformed JSON file
- **WHEN** a community ruleset file contains invalid JSON
- **THEN** the system SHALL log a warning with the filename and skip that file

### Requirement: Persistence event schema
The `RuleSetRegistryActor` SHALL use the following event types: `RuleSetLoaded(EntityKey, Hash, RuleSetFile, LoadedAtUtcTicks)`, `RuleSetUpdated(EntityKey, Hash, RuleSetFile, UpdatedAtUtcTicks)`, `RuleSetRemoved(EntityKey, RemovedAtUtcTicks)`, `LocalRegistered(...)`, `LocalRemoved(...)`. The full `RuleSetFile` content SHALL be stored in `RuleSetLoaded` and `RuleSetUpdated` events.

#### Scenario: RuleSetLoaded event content
- **WHEN** a new community file is loaded with entity key "83214" and hash "a1b2c3"
- **THEN** the persisted event SHALL contain `RuleSetLoaded("83214", "a1b2c3", <full RuleSetFile>, <current ticks>)`

#### Scenario: RuleSetUpdated event content
- **WHEN** a changed community file is detected with entity key "83214" and new hash "d4e5f6"
- **THEN** the persisted event SHALL contain `RuleSetUpdated("83214", "d4e5f6", <updated RuleSetFile>, <current ticks>)`

#### Scenario: RuleSetRemoved event content
- **WHEN** a community file for entity key "289363" is removed from disk
- **THEN** the persisted event SHALL contain `RuleSetRemoved("289363", <current ticks>)`

#### Scenario: Recovery state from events
- **WHEN** the journal contains `RuleSetLoaded("83214", "a1b2c3", ...)` followed by `RuleSetUpdated("83214", "d4e5f6", ...)`
- **THEN** the recovered state SHALL contain entity "83214" with hash "d4e5f6" and the updated content

### Requirement: Snapshot support
The `RuleSetRegistryActor` SHALL NOT use snapshot persistence. State SHALL be reconstructed purely from event replay. The event volume (initial load + periodic updates) is low enough that snapshots provide no measurable benefit.

#### Scenario: Recovery without snapshots
- **WHEN** the actor recovers with journaled events
- **THEN** it SHALL replay all events to reconstruct the hash map and ruleset content

#### Scenario: No snapshot creation
- **WHEN** any number of events have been persisted
- **THEN** no snapshot SHALL be created

## REMOVED Requirements

### Requirement: Snapshot support (from existing "Snapshot support" requirement)
**Reason**: Replaced by pure event replay. At ~60 initial events plus ~60 updates/year, recovery is fast without snapshots. Removing simplifies the actor.
**Migration**: Delete `RegistrySnapshot` record, `ToSnapshot`/`FromSnapshot` methods, `SaveSnapshotSuccess`/`SaveSnapshotFailure` handlers, and `SnapshotInterval` constant. Remove `CommunityBatchLoaded` event (replaced by per-file `RuleSetLoaded`/`RuleSetUpdated`).
