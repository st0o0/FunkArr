## MODIFIED Requirements

### Requirement: IdentificationStrategy enum
The `IdentificationStrategy` enum SHALL have 5 members with `[JsonPropertyName]` attributes matching the JSON ruleset schema strings:
- `SeasonAndEpisodeNumber` (`"seasonAndEpisodeNumber"`)
- `AbsoluteEpisodeNumber` (`"byAbsoluteEpisodeNumber"`)
- `TitleExact` (`"itemTitleExact"`)
- `TitleIncludes` (`"itemTitleIncludes"`)
- `AirdateExtraction` (`"itemTitleEqualsAirdate"`)

The enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<IdentificationStrategy>))]` for automatic deserialization from JSON ruleset files.

#### Scenario: Deserialize strategy from JSON
- **WHEN** a ruleset JSON contains `"strategy": "seasonAndEpisodeNumber"`
- **THEN** it deserializes to `IdentificationStrategy.SeasonAndEpisodeNumber` without manual switch logic

#### Scenario: All 5 strategies have JsonPropertyName
- **WHEN** the enum is inspected
- **THEN** each member has a `[JsonPropertyName]` matching the JSON schema string

### Requirement: TitleMatchMode deleted
The `TitleMatchMode` enum SHALL be deleted. The distinction between Exact and Contains is encoded in the `IdentificationStrategy` enum members `TitleExact` and `TitleIncludes`.

#### Scenario: No TitleMatchMode exists
- **WHEN** the codebase is searched for TitleMatchMode
- **THEN** no enum, file, or reference by that name exists

### Requirement: IdentificationSpec without MatchMode
The `IdentificationSpec` record SHALL NOT have a `MatchMode` parameter. The strategy enum alone determines matching behavior.

#### Scenario: IdentificationSpec has no MatchMode
- **WHEN** `IdentificationSpec` is inspected
- **THEN** it has parameters: Strategy, SeasonPattern, EpisodePattern, CaptureGroup, TitleParts — no MatchMode

### Requirement: FilterField with JsonStringEnumConverter
The `FilterField` enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<FilterField>))]` with camelCase naming for automatic deserialization from JSON ruleset files.

#### Scenario: Deserialize filter field from JSON
- **WHEN** a ruleset JSON contains `"field": "title"`
- **THEN** it deserializes to `FilterField.Title` without manual switch logic

### Requirement: FilterOp with JsonStringEnumConverter
The `FilterOp` enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<FilterOp>))]` with camelCase naming for automatic deserialization from JSON ruleset files.

#### Scenario: Deserialize filter op from JSON
- **WHEN** a ruleset JSON contains `"op": "greaterThan"`
- **THEN** it deserializes to `FilterOp.GreaterThan` without manual switch logic

### Requirement: TitlePartType with JsonStringEnumConverter
The `TitlePartType` enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<TitlePartType>))]` with camelCase naming for automatic deserialization from JSON ruleset files.

#### Scenario: Deserialize title part type from JSON
- **WHEN** a ruleset JSON contains `"type": "regex"`
- **THEN** it deserializes to `TitlePartType.Regex` without manual switch logic

### Requirement: RuleSetMerger uses typed enums
The `RuleSetMerger` internal classes (`RawRule`, `RawTitleRule`) SHALL use enum-typed properties instead of strings for `Strategy`, `Field`, `Op`, and `Type`. The manual `ParseFilterField`, `ParseFilterOp` switch methods SHALL be removed.

#### Scenario: RawRule.Strategy is enum
- **WHEN** `RawRule` is inspected
- **THEN** `Strategy` is `IdentificationStrategy?` not `string`

#### Scenario: No ParseFilterField method
- **WHEN** `RuleSetMerger` is inspected
- **THEN** no methods named ParseFilterField or ParseFilterOp exist
