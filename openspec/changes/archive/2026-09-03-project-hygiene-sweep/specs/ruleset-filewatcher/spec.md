## MODIFIED Requirements

### Requirement: RuleSetManager monitors ruleset directories with FileSystemWatcher
The RuleSetManager SHALL inject `IOptionsMonitor<FunkArrOptions>` via DI and derive its watched directories from `FunkArrOptions.RuleSetDataPath` and `FunkArrOptions.LocalRuleSetDataPath` in `PreStart()`. It SHALL create two `FileSystemWatcher` instances: one for `{RuleSetDataPath}/rulesets/` and one for `{LocalRuleSetDataPath}/rulesets/`. Both SHALL watch for `*.json` file changes. All paths SHALL be resolved via `Path.GetFullPath` at initialization.

#### Scenario: Watchers start on PreStart
- **WHEN** the RuleSetManager actor starts
- **THEN** it SHALL read paths from `IOptionsMonitor<FunkArrOptions>.CurrentValue`
- **AND** two FileSystemWatchers SHALL be active, monitoring the resolved community and local ruleset directories for `*.json` files

#### Scenario: Watcher created for non-existent directory
- **WHEN** the local rulesets directory does not exist at startup
- **THEN** the RuleSetManager SHALL create the directory and start the watcher

#### Scenario: Watchers disposed on PostStop
- **WHEN** the RuleSetManager actor stops
- **THEN** both FileSystemWatcher instances SHALL be disposed

### Requirement: RuleSetManager holds known ruleset state
The RuleSetManager SHALL maintain its known ruleset state using `ImmutableDictionary<string, RuleSetPaths>` and pending IDs using `ImmutableHashSet<string>`, consistent with other state records in the project.

#### Scenario: State populated on startup scan
- **WHEN** the RuleSetManager performs its initial scan and finds 62 community files and 3 local files
- **THEN** the known state SHALL contain entries for all unique ruleset IDs with their respective paths

#### Scenario: State updated after FlushChanges
- **WHEN** `FlushChanges` detects a new file `new-show.json` in community
- **THEN** the known state SHALL include `new-show` after dispatching `LoadRuleSet`

#### Scenario: State mutations are immutable
- **WHEN** `PendingIds.Add(id)` is called
- **THEN** a new `ImmutableHashSet` SHALL be produced and assigned via `_state = _state with { ... }`
- **AND** the previous state reference SHALL remain unchanged
