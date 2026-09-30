## MODIFIED Requirements

### Requirement: RuleSetManager scans ruleset directories at startup
The RuleSetManager (Singleton) SHALL inject `IOptionsMonitor<FunkArrOptions>` and derive directory paths from `FunkArrOptions.RuleSetDataPath` and `FunkArrOptions.LocalRuleSetDataPath` in `PreStart()`. It SHALL scan `{RuleSetDataPath}/rulesets/*.json` and `{LocalRuleSetDataPath}/rulesets/*.json` at startup and activate a RuleSetWorker for each discovered ruleSetId. `ScanRuleSets` becomes a parameterless internal message for triggering re-scans; initialization happens directly in `PreStart()`.

#### Scenario: Startup with community rulesets only
- **WHEN** the system starts with 5 JSON files in `{RuleSetDataPath}/rulesets/`
- **THEN** 5 RuleSetWorkers are activated via `LoadRuleSet`, one per file, using the filename (without extension) as ruleSetId

#### Scenario: Startup with community and local rulesets
- **WHEN** a ruleSetId exists in both community and local directories
- **THEN** one `LoadRuleSet` is sent for that ruleSetId, containing both file paths

#### Scenario: Local-only ruleset
- **WHEN** a ruleSetId exists only in `{LocalRuleSetDataPath}/rulesets/`
- **THEN** a `LoadRuleSet` is sent for that ruleSetId with only the local file path

#### Scenario: Query detail for known ruleset
- **WHEN** `QueryRuleSetDetail("tatort")` is received and "tatort" is in KnownRuleSets
- **THEN** the Manager re-reads the community and/or local JSON files, runs `RuleSetMerger.ExtractIdentity()` and `RuleSetMerger.Build()`, and responds with a `RuleSetDetailResult` containing identity, source metadata (paths, timestamps), and the merged matching config

#### Scenario: Query detail for unknown ruleset
- **WHEN** `QueryRuleSetDetail("nonexistent")` is received and the ruleSetId is not in KnownRuleSets
- **THEN** the Manager responds with `RuleSetNotFound("nonexistent")`

#### Scenario: Query detail when file was deleted after load
- **WHEN** `QueryRuleSetDetail("tatort")` is received but the JSON file has been deleted since the last scan
- **THEN** the Manager responds with `RuleSetNotFound("tatort")` and removes the entry from KnownRuleSets

## ADDED Requirements

### Requirement: FunkArrOptions exposes local ruleset path
`FunkArrOptions` SHALL expose a `LocalRuleSetDataPath` computed property returning `Path.Combine(DataPath, "local")`, complementing the existing `RuleSetDataPath` (which returns `Path.Combine(DataPath, "community")`).

#### Scenario: LocalRuleSetDataPath derivation
- **WHEN** `DataPath` is `"data"`
- **THEN** `LocalRuleSetDataPath` SHALL return `"data/local"`

#### Scenario: Custom DataPath propagates
- **WHEN** `DataPath` is `"/app/config"`
- **THEN** `LocalRuleSetDataPath` SHALL return `"/app/config/local"`
- **AND** `RuleSetDataPath` SHALL return `"/app/config/community"`
