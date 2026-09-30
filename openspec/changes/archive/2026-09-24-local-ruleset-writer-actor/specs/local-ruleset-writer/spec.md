## ADDED Requirements

### Requirement: LocalRuleSetWriter actor handles create operations
The `LocalRuleSetWriter` actor SHALL accept `CreateLocalRuleSet` commands and respond with `CreateLocalRuleSetCompleted` on success or `CreateLocalRuleSetFailed` on failure. The actor SHALL check for existing local files, convert the message to disk-format JSON using `RuleSetWriter`, validate against the JSON schema via `IRuleSetValidator`, create the local rulesets directory if needed, and write the file atomically via `IDataFiles.WriteAtomic`.

#### Scenario: Successful create
- **WHEN** `CreateLocalRuleSet` is received with ruleSetId "test-show" and valid data
- **THEN** the actor SHALL write `{LocalRuleSets}/test-show.json` with string-enum disk format and respond with `CreateLocalRuleSetCompleted("test-show")`

#### Scenario: Create with duplicate ID
- **WHEN** `CreateLocalRuleSet` is received with ruleSetId "tatort" and a local file already exists at `{LocalRuleSets}/tatort.json`
- **THEN** the actor SHALL respond with `CreateLocalRuleSetFailed` with reason `AlreadyExists`

#### Scenario: Create with schema validation failure
- **WHEN** `CreateLocalRuleSet` is received with data that fails JSON schema validation
- **THEN** the actor SHALL respond with `CreateLocalRuleSetFailed` with reason `ValidationFailed` containing the validation errors

### Requirement: LocalRuleSetWriter actor handles update operations
The `LocalRuleSetWriter` actor SHALL accept `UpdateLocalRuleSet` commands. The actor SHALL verify that either a community or local file exists for the given ruleSetId before writing.

#### Scenario: Successful update of community ruleset
- **WHEN** `UpdateLocalRuleSet` is received for ruleSetId "tatort" which has a community file but no local file
- **THEN** the actor SHALL write `{LocalRuleSets}/tatort.json` (creating a local overlay) and respond with `UpdateLocalRuleSetCompleted`

#### Scenario: Successful update of existing local ruleset
- **WHEN** `UpdateLocalRuleSet` is received for ruleSetId "test-show" which has a local file
- **THEN** the actor SHALL overwrite `{LocalRuleSets}/test-show.json` and respond with `UpdateLocalRuleSetCompleted`

#### Scenario: Update for nonexistent ruleset
- **WHEN** `UpdateLocalRuleSet` is received for ruleSetId "nonexistent" with no community or local file
- **THEN** the actor SHALL respond with `UpdateLocalRuleSetFailed` with reason `NotFound`

#### Scenario: Update with schema validation failure
- **WHEN** `UpdateLocalRuleSet` is received with data that fails JSON schema validation
- **THEN** the actor SHALL respond with `UpdateLocalRuleSetFailed` with reason `ValidationFailed` containing the validation errors

### Requirement: LocalRuleSetWriter actor handles delete operations
The `LocalRuleSetWriter` actor SHALL accept `DeleteLocalRuleSet` commands. The actor SHALL only delete the local file, never the community file.

#### Scenario: Delete local overlay (merged ruleset)
- **WHEN** `DeleteLocalRuleSet` is received for ruleSetId "tatort" which has both community and local files
- **THEN** the actor SHALL delete only `{LocalRuleSets}/tatort.json` and respond with `DeleteLocalRuleSetCompleted`

#### Scenario: Delete local-only ruleset
- **WHEN** `DeleteLocalRuleSet` is received for ruleSetId "test-show" which has only a local file
- **THEN** the actor SHALL delete `{LocalRuleSets}/test-show.json` and respond with `DeleteLocalRuleSetCompleted`

#### Scenario: Delete nonexistent local file
- **WHEN** `DeleteLocalRuleSet` is received for ruleSetId "community-only" which has no local file
- **THEN** the actor SHALL respond with `DeleteLocalRuleSetFailed` with reason `NotFound`

### Requirement: LocalRuleSetWriter actor handles export operations
The `LocalRuleSetWriter` actor SHALL accept `ExportRuleSet` commands. Export SHALL read the local file, flatten it (merge with community if applicable), validate, and return the standalone JSON.

#### Scenario: Export merged ruleset
- **WHEN** `ExportRuleSet` is received for ruleSetId "tatort" which has both community and local files
- **THEN** the actor SHALL merge the files, validate the result, and respond with `ExportRuleSetCompleted` containing the flattened JSON

#### Scenario: Export local-only ruleset
- **WHEN** `ExportRuleSet` is received for ruleSetId "test-show" which has only a local file
- **THEN** the actor SHALL validate and respond with `ExportRuleSetCompleted` containing the JSON

#### Scenario: Export ruleset without local component
- **WHEN** `ExportRuleSet` is received for a ruleSetId with no local file
- **THEN** the actor SHALL respond with `ExportRuleSetFailed` with reason `NotFound`

### Requirement: RuleSetWriter service converts messages to disk format
The `RuleSetWriter` class (non-actor) SHALL convert domain message types to disk-format JSON strings. It SHALL use `RuleSetEnumMapping` to convert domain enum values to their disk string representations. The resulting JSON SHALL conform to the ruleset JSON schema.

#### Scenario: Strategy enum conversion
- **WHEN** a rule with `IdentificationStrategy.TitleIncludes` is serialized
- **THEN** the disk JSON SHALL contain `"strategy": "itemTitleIncludes"`

#### Scenario: Filter field enum conversion
- **WHEN** a filter with `FilterField.Title` and `FilterOp.Contains` is serialized
- **THEN** the disk JSON SHALL contain `"field": "title"` and `"op": "contains"`

#### Scenario: Title rule type enum conversion
- **WHEN** a title rule with `TitlePartType.Static` is serialized
- **THEN** the disk JSON SHALL contain `"type": "static"`

#### Scenario: Enrichment method enum conversion
- **WHEN** enrichment methods `[EnrichmentMethod.Title, EnrichmentMethod.Airdate]` are serialized
- **THEN** the disk JSON SHALL contain `"methods": ["title", "airdate"]`

### Requirement: RuleSetEnumMapping provides bidirectional conversion
The `RuleSetEnumMapping` class SHALL provide static methods for converting between domain enum values and their disk string representations in both directions. It SHALL cover `IdentificationStrategy`, `FilterField`, `FilterOp`, `TitlePartType`, `EnrichmentMethod`, `RuntimeMode`, and `MediaType`.

#### Scenario: Strategy string to enum
- **WHEN** `"itemTitleIncludes"` is mapped to enum
- **THEN** the result SHALL be `IdentificationStrategy.TitleIncludes`

#### Scenario: Strategy enum to string
- **WHEN** `IdentificationStrategy.TitleIncludes` is mapped to string
- **THEN** the result SHALL be `"itemTitleIncludes"`

#### Scenario: Unknown strategy string
- **WHEN** an unrecognized strategy string is mapped
- **THEN** the mapping SHALL return false (TryParse pattern)

### Requirement: RuleSetMerger renamed to RuleSetReader
The `RuleSetMerger` class SHALL be renamed to `RuleSetReader`. All references SHALL be updated. The `TryParseStrategy` method SHALL be extracted to `RuleSetEnumMapping`.

#### Scenario: Reader class exists
- **WHEN** the codebase is inspected
- **THEN** `RuleSetReader.cs` SHALL exist and `RuleSetMerger.cs` SHALL NOT exist

#### Scenario: Reader uses shared enum mapping
- **WHEN** `RuleSetReader` parses a strategy string from disk JSON
- **THEN** it SHALL use `RuleSetEnumMapping.TryParseStrategy` instead of an inline switch
