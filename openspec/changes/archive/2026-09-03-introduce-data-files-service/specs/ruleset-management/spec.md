## MODIFIED Requirements

### Requirement: RuleSetManager scans ruleset directories at startup
The RuleSetManager (Singleton) SHALL inject `IDataFiles` and `DataPaths` and use `DataPaths.CommunityRuleSets` and `DataPaths.LocalRuleSets` for directory paths. It SHALL scan using `IDataFiles.ListFiles(directory, "*.json")` at startup and activate a RuleSetWorker for each discovered ruleSetId.

#### Scenario: Startup with community rulesets only
- **WHEN** the system starts with 5 JSON files in `DataPaths.CommunityRuleSets`
- **THEN** 5 RuleSetWorkers are activated via `LoadRuleSet`, one per file, using the filename (without extension) as ruleSetId

#### Scenario: Startup with community and local rulesets
- **WHEN** a ruleSetId exists in both community and local directories
- **THEN** one `LoadRuleSet` is sent for that ruleSetId, containing both file paths

#### Scenario: Local-only ruleset
- **WHEN** a ruleSetId exists only in `DataPaths.LocalRuleSets`
- **THEN** a `LoadRuleSet` is sent for that ruleSetId with only the local file path

#### Scenario: Query detail for known ruleset
- **WHEN** `QueryRuleSetDetail("tatort")` is received and "tatort" is in KnownRuleSets
- **THEN** the Manager re-reads the community and/or local JSON files using `IDataFiles.ReadText()`, runs `RuleSetMerger.ExtractIdentity()` and `RuleSetMerger.Build()`, and responds with a `RuleSetDetailResult`

#### Scenario: Query detail for unknown ruleset
- **WHEN** `QueryRuleSetDetail("nonexistent")` is received and the ruleSetId is not in KnownRuleSets
- **THEN** the Manager responds with `RuleSetNotFound("nonexistent")`

#### Scenario: Query detail when file was deleted after load
- **WHEN** `QueryRuleSetDetail("tatort")` is received but `IDataFiles.Exists()` returns false for the JSON file
- **THEN** the Manager responds with `RuleSetNotFound("tatort")` and removes the entry from KnownRuleSets

### Requirement: RuleSetWorker loads and merges ruleset files
Each RuleSetWorker (Sharded by ruleSetId) SHALL handle `LoadRuleSet` messages by loading its community and/or local JSON file(s) using `IDataFiles.ReadText()` after checking `IDataFiles.Exists()`, merging them using the existing resolve logic, and producing a MatchingConfig message.

#### Scenario: Community-only ruleset
- **WHEN** a RuleSetWorker receives `LoadRuleSet` with only a community JSON path
- **THEN** it reads the file via `IDataFiles.ReadText()` and produces a MatchingConfig from that file alone

#### Scenario: Community + local merge
- **WHEN** a RuleSetWorker receives `LoadRuleSet` with both community and local JSON paths
- **THEN** it reads both files via `IDataFiles.ReadText()` and merges them (local overrides community)

#### Scenario: Re-load with updated file
- **WHEN** a RuleSetWorker receives a second `LoadRuleSet` for the same ruleSetId with a modified community file
- **THEN** it SHALL re-read via `IDataFiles.ReadText()`, re-merge, and re-push the updated MatchingConfig

### Requirement: FunkArrOptions exposes data path only
`FunkArrOptions` SHALL expose only `ApiKey` (string) and `DataPath` (string). The computed path properties `RuleSetDataPath`, `LocalRuleSetDataPath`, and `PersistencePath` are removed — path resolution is handled by `DataPaths`.

#### Scenario: FunkArrOptions simplified
- **WHEN** `FunkArrOptions` is inspected
- **THEN** it SHALL have `ApiKey` and `DataPath` properties only
- **AND** it SHALL NOT have `PersistencePath`, `RuleSetDataPath`, or `LocalRuleSetDataPath`
