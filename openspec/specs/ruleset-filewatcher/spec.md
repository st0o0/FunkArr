# ruleset-filewatcher

## Purpose

FileSystemWatcher-based live reload for the RuleSetManager — monitors community and local ruleset directories for changes, debounces events, and dispatches LoadRuleSet/RemoveRuleSet to workers.

## Requirements

### Requirement: RuleSetManager monitors ruleset directories with FileSystemWatcher
The RuleSetManager SHALL create two `FileSystemWatcher` instances on startup: one for `data/community/rulesets/` and one for `data/local/rulesets/`. Both SHALL watch for `*.json` file changes (Created, Changed, Deleted, Renamed events).

#### Scenario: Watchers start on PreStart
- **WHEN** the RuleSetManager actor starts
- **THEN** two FileSystemWatchers SHALL be active, monitoring `{DataPath}/community/rulesets/` and `{DataPath}/local/rulesets/` for `*.json` files

#### Scenario: Watcher created for non-existent directory
- **WHEN** `data/local/rulesets/` does not exist at startup
- **THEN** the RuleSetManager SHALL create the directory and start the watcher

#### Scenario: Watchers disposed on PostStop
- **WHEN** the RuleSetManager actor stops
- **THEN** both FileSystemWatcher instances SHALL be disposed

### Requirement: FileSystemWatcher events are debounced via scheduler
The RuleSetManager SHALL debounce FileSystemWatcher events by accumulating affected ruleset IDs in a `HashSet<string>` and scheduling a `FlushChanges` message after 2 seconds. The ruleset ID SHALL be extracted from the changed file's name (`Path.GetFileNameWithoutExtension`). Multiple events for the same ID within the debounce window SHALL be deduplicated.

#### Scenario: Single file change accumulates one ID
- **WHEN** `tatort.json` is modified in `data/community/rulesets/`
- **THEN** the RuleSetManager SHALL add `"tatort"` to the pending IDs set and schedule a `FlushChanges` message after 2 seconds

#### Scenario: Multiple changes to same file collapse into one ID
- **WHEN** `tatort.json` is modified 5 times in `data/community/rulesets/` within 1 second
- **THEN** the pending IDs set SHALL contain only one entry for `"tatort"` and exactly one `FlushChanges` SHALL fire

#### Scenario: Changes to different files accumulate multiple IDs
- **WHEN** `tatort.json` and `tagesschau.json` are both modified within 1 second
- **THEN** the pending IDs set SHALL contain both `"tatort"` and `"tagesschau"` and one `FlushChanges` SHALL process both

#### Scenario: Events during debounce window do not reset timer
- **WHEN** a file change event occurs while a `FlushChanges` is already scheduled
- **THEN** the existing timer SHALL NOT be reset — `FlushChanges` fires at the originally scheduled time, and the new ID is added to the pending set

#### Scenario: Change in community and local for same ID
- **WHEN** `tatort.json` is modified in both `data/community/rulesets/` and `data/local/rulesets/` within the debounce window
- **THEN** the pending IDs set SHALL contain one entry for `"tatort"` and flush SHALL check both paths

### Requirement: FlushChanges checks only affected rulesets
On `FlushChanges`, the RuleSetManager SHALL check file existence and timestamps only for the accumulated pending ruleset IDs, not for all known rulesets. For each pending ID, it SHALL check both `{communityDir}/{id}.json` and `{localDir}/{id}.json`, build the current `RuleSetPaths`, and compare against the known state to dispatch `LoadRuleSet` or `RemoveRuleSet`.

#### Scenario: New ruleset detected via targeted check
- **WHEN** `FlushChanges` fires with pending ID `"new-show"` and `new-show.json` exists in `data/community/rulesets/` but is not in known state
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("new-show", communityPath, null)` to the shard region

#### Scenario: Changed ruleset detected via targeted check
- **WHEN** `FlushChanges` fires with pending ID `"tatort"` and the file timestamp differs from known state
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("tatort", communityPath, localPath)` with current paths

#### Scenario: Ruleset removed detected via targeted check
- **WHEN** `FlushChanges` fires with pending ID `"custom-show"` and the file no longer exists in either directory
- **THEN** the RuleSetManager SHALL send `RemoveRuleSet("custom-show")` and remove it from known state

#### Scenario: No actual change for pending ID
- **WHEN** `FlushChanges` fires with pending ID `"tatort"` but the file timestamps match known state exactly
- **THEN** the RuleSetManager SHALL NOT send any message for that ID

#### Scenario: Rename detected as remove + add
- **WHEN** a file is renamed from `old-name.json` to `new-name.json` in `data/community/rulesets/`
- **THEN** both `"old-name"` and `"new-name"` SHALL be in the pending set; flush SHALL send `RemoveRuleSet("old-name")` and `LoadRuleSet("new-name", ...)`

#### Scenario: Pending set cleared after flush
- **WHEN** `FlushChanges` completes processing all pending IDs
- **THEN** the pending IDs set SHALL be empty and no `FlushChanges` SHALL be scheduled

### Requirement: RuleSetManager handles FileSystemWatcher errors
The RuleSetManager SHALL handle `FileSystemWatcher.Error` events by scheduling a full directory rescan via `FlushChanges`. A `FullRescanRequested` flag SHALL distinguish error-triggered flushes from targeted flushes, causing the flush handler to fall back to `ScanDirectories()` and diff the full result against known state.

#### Scenario: Watcher buffer overflow triggers full rescan
- **WHEN** a FileSystemWatcher raises an Error event due to internal buffer overflow
- **THEN** the RuleSetManager SHALL set `FullRescanRequested = true` and schedule a `FlushChanges` message

#### Scenario: Full rescan diffs entire directory
- **WHEN** `FlushChanges` fires with `FullRescanRequested = true`
- **THEN** the RuleSetManager SHALL call `ScanDirectories()`, diff the full result against known state, and dispatch `LoadRuleSet`/`RemoveRuleSet` for all differences

#### Scenario: Full rescan clears pending IDs
- **WHEN** `FlushChanges` fires with `FullRescanRequested = true` and pending IDs exist
- **THEN** the full rescan SHALL process everything — pending IDs are irrelevant and SHALL be cleared

### Requirement: RuleSetManager holds known ruleset state
The RuleSetManager SHALL maintain a dictionary of known ruleset IDs mapped to their community and local file paths. This state is populated on initial scan and updated on each `FlushChanges`.

#### Scenario: State populated on startup scan
- **WHEN** the RuleSetManager performs its initial scan and finds 62 community files and 3 local files
- **THEN** the known state SHALL contain entries for all unique ruleset IDs with their respective paths

#### Scenario: State updated after FlushChanges
- **WHEN** `FlushChanges` detects a new file `new-show.json` in community
- **THEN** the known state SHALL include `new-show` after dispatching `LoadRuleSet`
