## ADDED Requirements

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
The RuleSetManager SHALL debounce FileSystemWatcher events using a dirty flag and a 500ms scheduled `FlushChanges` message. Multiple events within the debounce window SHALL result in a single re-scan.

#### Scenario: Single file change triggers one re-scan
- **WHEN** a single JSON file is modified in `data/community/rulesets/`
- **THEN** the RuleSetManager SHALL schedule a `FlushChanges` message after 500ms and perform exactly one directory re-scan

#### Scenario: Multiple rapid events collapse into one re-scan
- **WHEN** 60 JSON files are created in `data/community/rulesets/` within 100ms (e.g., atomic directory swap)
- **THEN** the RuleSetManager SHALL perform exactly one directory re-scan after the 500ms debounce window

#### Scenario: Events during debounce window do not reset timer
- **WHEN** a file change event occurs while a `FlushChanges` is already scheduled
- **THEN** the existing timer SHALL NOT be reset — `FlushChanges` fires at the originally scheduled time

### Requirement: FlushChanges re-scans directories and diffs against known state
On `FlushChanges`, the RuleSetManager SHALL re-scan both directories, produce a current ruleset map, and diff against the known state to determine new, changed, and removed rulesets.

#### Scenario: New ruleset detected
- **WHEN** a JSON file `new-show.json` appears in `data/community/rulesets/` that was not in the known state
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("new-show", communityPath, null)` to the shard region

#### Scenario: Changed ruleset detected
- **WHEN** an existing JSON file `tatort.json` is modified in `data/community/rulesets/`
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("tatort", communityPath, localPath)` to the shard region with current paths

#### Scenario: Local override added
- **WHEN** a JSON file `tatort.json` appears in `data/local/rulesets/` while `tatort.json` already exists in `data/community/rulesets/`
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("tatort", communityPath, localPath)` with both paths

#### Scenario: Ruleset removed from both directories
- **WHEN** `custom-show.json` is deleted and existed only in `data/local/rulesets/`
- **THEN** the RuleSetManager SHALL send `RemoveRuleSet("custom-show")` to the shard region

#### Scenario: Local override removed, community remains
- **WHEN** `tatort.json` is deleted from `data/local/rulesets/` but still exists in `data/community/rulesets/`
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("tatort", communityPath, null)` to re-merge with community-only

#### Scenario: No changes detected
- **WHEN** `FlushChanges` fires but the directory contents match the known state exactly
- **THEN** the RuleSetManager SHALL NOT send any messages to the shard region

### Requirement: RuleSetManager handles FileSystemWatcher errors
The RuleSetManager SHALL handle `FileSystemWatcher.Error` events by setting the dirty flag and scheduling a re-scan. This handles buffer overflow scenarios where events are lost.

#### Scenario: Watcher buffer overflow
- **WHEN** a FileSystemWatcher raises an Error event due to internal buffer overflow
- **THEN** the RuleSetManager SHALL set `_dirty = true` and schedule a `FlushChanges` message

### Requirement: RuleSetManager holds known ruleset state
The RuleSetManager SHALL maintain a dictionary of known ruleset IDs mapped to their community and local file paths. This state is populated on initial scan and updated on each `FlushChanges`.

#### Scenario: State populated on startup scan
- **WHEN** the RuleSetManager performs its initial scan and finds 62 community files and 3 local files
- **THEN** the known state SHALL contain entries for all unique ruleset IDs with their respective paths

#### Scenario: State updated after FlushChanges
- **WHEN** `FlushChanges` detects a new file `new-show.json` in community
- **THEN** the known state SHALL include `new-show` after dispatching `LoadRuleSet`
