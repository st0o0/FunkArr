## RENAMED Requirements

### Requirement: RuleSetManager scans ruleset directories at startup
- **FROM:** References to `RuleSetMerger` in scan/query logic
- **TO:** References to `RuleSetReader` (class rename only, behavior unchanged)

## MODIFIED Requirements

### Requirement: RuleSetManager scans ruleset directories at startup
The RuleSetManager (Singleton) SHALL inject `IDataFiles` and `DataPaths` and use `DataPaths.CommunityRuleSets` and `DataPaths.LocalRuleSets` for directory paths. It SHALL scan using `IDataFiles.ListFiles(directory, "*.json")` at startup and activate a RuleSetWorker for each discovered ruleSetId. The Manager SHALL use `RuleSetReader` (renamed from `RuleSetMerger`) for reading and merging ruleset files. `RuleSetReader` SHALL use `RuleSetEnumMapping` for strategy parsing instead of an inline `TryParseStrategy` switch.

#### Scenario: Startup with community rulesets only
- **WHEN** the system starts with 5 JSON files in `DataPaths.CommunityRuleSets`
- **THEN** 5 RuleSetWorkers are activated via `LoadRuleSet`, one per file, using the filename (without extension) as ruleSetId

#### Scenario: Query detail uses RuleSetReader
- **WHEN** `QueryRuleSetDetail("tatort")` is received
- **THEN** the Manager SHALL use `RuleSetReader` (not `RuleSetMerger`) to read and merge the ruleset files

### Requirement: RuleSetWorker loads and merges ruleset files
Each RuleSetWorker (Sharded by ruleSetId) SHALL handle `LoadRuleSet` messages by loading its community and/or local JSON file(s) using `IDataFiles.ReadText()` after checking `IDataFiles.Exists()`, merging them using `RuleSetReader` (renamed from `RuleSetMerger`).

#### Scenario: Worker uses RuleSetReader
- **WHEN** a RuleSetWorker receives `LoadRuleSet`
- **THEN** it SHALL call `RuleSetReader` methods (not `RuleSetMerger`) to load and merge the ruleset
