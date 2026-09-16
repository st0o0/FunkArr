## Purpose

Shared typed input records for RuleSet data used across Create, Update, and TestScore API endpoints, eliminating duplicate type definitions.

## Requirements

### Requirement: Shared RuleInput record
The API SHALL define a `RuleInput` record used by both RuleSet CRUD and TestScore endpoints. It SHALL have: `Id` (string, required), `Priority` (int), `Confidence` (float?), `Strategy` (IdentificationStrategy), `SeasonRegex` (string?), `EpisodeRegex` (string?), `CaptureGroup` (int?), `Filters` (FilterGroupInput?), `TitleRules` (TitleRuleInput[]?).

#### Scenario: RuleInput used in CreateRuleSet
- **WHEN** a Create request contains a rule with strategy `seasonAndEpisodeNumber`
- **THEN** `RuleInput.Strategy` deserializes to `IdentificationStrategy.SeasonAndEpisodeNumber`

#### Scenario: RuleInput used in TestScore
- **WHEN** a TestScore request contains the same rule structure
- **THEN** the same `RuleInput` type is used, no separate TestRule type exists

### Requirement: Shared MediaInput record
The API SHALL define a `MediaInput` record with: `Name` (string, required), `Type` (string, required), `TvdbId` (int?), `ImdbId` (string?), `TmdbId` (int?).

#### Scenario: MediaInput with optional IDs
- **WHEN** a request contains media with only name and type
- **THEN** the optional ID fields are null and omitted from serialized JSON

### Requirement: Shared FilterGroupInput record
The API SHALL define a `FilterGroupInput` record with: `All` (FilterNodeInput[]?), `Any` (FilterNodeInput[]?), `Not` (FilterNodeInput[]?).

#### Scenario: Filter group with all section
- **WHEN** a request contains filters with only an `all` section
- **THEN** `Any` and `Not` are null

### Requirement: Shared FilterNodeInput record
The API SHALL define a `FilterNodeInput` record that represents either a condition or a nested group. Condition fields: `Field` (FilterField?), `Op` (FilterOp?), `Value` (string?). Group fields: `All`, `Any`, `Not` (FilterNodeInput[]?).

#### Scenario: Condition node
- **WHEN** a filter node has field, op, and value
- **THEN** it represents a condition

#### Scenario: Nested group node
- **WHEN** a filter node has all/any/not arrays
- **THEN** it represents a nested filter group

### Requirement: Shared TitleRuleInput record
The API SHALL define a `TitleRuleInput` record with: `Type` (TitlePartType, required), `Field` (FilterField?), `Pattern` (string?), `CaptureGroup` (int?), `Value` (string?).

#### Scenario: Static title rule
- **WHEN** a title rule has type `static` and a value
- **THEN** `Type` is `TitlePartType.Static` and `Value` contains the text

### Requirement: No duplicate TestRule types
The old `TestRule`, `TestRuleConfig`, `TestFilterSpec`, `TestFilterNode`, `TestTitleRule` types SHALL be deleted. Their functionality is replaced by the shared input records.

#### Scenario: No TestRule class exists
- **WHEN** the codebase is searched for `TestRule`, `TestFilterSpec`, `TestFilterNode`, `TestTitleRule`
- **THEN** no types by those names exist
