## Purpose

Centralized disk I/O service for ruleset files. Encapsulates all file path resolution, serialization, and directory scanning so no other class in `FunkArr.RuleSet` touches disk directly.

## Requirements

### Requirement: RuleSetStore provides centralized disk I/O
The `RuleSetStore` class SHALL be a DI-registered service that encapsulates all ruleset disk operations. It SHALL inject `IDataFiles`, `DataPaths`, and use `DiskJsonOptions` for serialization. No other class in `FunkArr.RuleSet` SHALL build ruleset file paths or call `IDataFiles` directly for ruleset operations.

#### Scenario: Load community and local
- **WHEN** `Load("tatort")` is called and both community and local files exist
- **THEN** the store SHALL return two deserialized `DiskRuleSet` instances (community, local)

#### Scenario: Load community only
- **WHEN** `Load("tatort")` is called and only the community file exists
- **THEN** the store SHALL return `(community: DiskRuleSet, local: null)`

#### Scenario: Load nonexistent
- **WHEN** `Load("nonexistent")` is called and neither file exists
- **THEN** the store SHALL return `(null, null)`

### Requirement: RuleSetStore provides merged loading
The `LoadMerged(id)` method SHALL deserialize both files, call `RuleSetMerger.Resolve`, and return the merged `DiskRuleSet`. This replaces all scattered deserialize-then-merge patterns.

#### Scenario: Merged loading
- **WHEN** `LoadMerged("tatort")` is called with community and local files
- **THEN** the store SHALL return a single merged `DiskRuleSet` resolved by `RuleSetMerger`

### Requirement: RuleSetStore handles local writes
The `SaveLocal(id, DiskRuleSet)` method SHALL serialize the record to JSON using `DiskJsonOptions`, create the local directory if needed, write atomically, and return the serialized JSON string (for validation by the caller).

#### Scenario: Save local ruleset
- **WHEN** `SaveLocal("test-show", diskRuleSet)` is called
- **THEN** the store SHALL write `{LocalRuleSets}/test-show.json` atomically and return the JSON string

### Requirement: RuleSetStore handles local deletes
The `DeleteLocal(id)` method SHALL remove the local file and return whether it existed.

#### Scenario: Delete existing local
- **WHEN** `DeleteLocal("test-show")` is called and the local file exists
- **THEN** the store SHALL delete the file and return true

#### Scenario: Delete nonexistent local
- **WHEN** `DeleteLocal("nonexistent")` is called and no local file exists
- **THEN** the store SHALL return false

### Requirement: RuleSetStore provides directory scanning
The `Scan()` method SHALL list all JSON files in community and local directories and return a dictionary of `RuleSetPaths` keyed by ruleSetId.

#### Scenario: Scan with community and local files
- **WHEN** `Scan()` is called with 5 community and 2 local files (1 overlapping)
- **THEN** the store SHALL return 6 entries with correct `CommunityPath`/`LocalPath` combinations

### Requirement: RuleSetStore provides existence checks
The store SHALL provide `ExistsLocal(id)` and `ExistsCommunity(id)` methods.

#### Scenario: Check existence
- **WHEN** `ExistsLocal("tatort")` is called and a local file exists
- **THEN** the store SHALL return true
