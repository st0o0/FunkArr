## MODIFIED Requirements

### Requirement: TestScoreMappingExtensions simplified
The `TestScoreMappingExtensions` SHALL use shared `RuleInput` types. The `ToMessage()` extension on `TestScoreRequest` SHALL map `RuleInput` to `MatchingRule` using the same logic, but with less mapping code since field/op/type are already enums.

#### Scenario: RuleInput.Strategy maps directly
- **WHEN** a `RuleInput` with `Strategy = IdentificationStrategy.TitleIncludes` is mapped
- **THEN** the resulting `IdentificationSpec` has `Strategy = IdentificationStrategy.TitleIncludes` without string parsing

#### Scenario: FilterNodeInput.Field maps directly
- **WHEN** a `FilterNodeInput` with `Field = FilterField.Duration` is mapped
- **THEN** the resulting `FilterCondition` has `Field = FilterField.Duration` without string parsing

### Requirement: Disk serialization extension
The API SHALL provide a `ToDiskJson()` extension or helper that serializes a Create/Update request to JSON suitable for disk storage, using `DefaultIgnoreCondition = WhenWritingNull`, `CamelCase` naming, and `WriteIndented = true`.

#### Scenario: Null fields omitted
- **WHEN** a request with `Aliases = null` is serialized for disk
- **THEN** the JSON output does not contain an `aliases` key

#### Scenario: Enum fields as strings in JSON
- **WHEN** a request with `Strategy = IdentificationStrategy.SeasonAndEpisodeNumber` is serialized for disk
- **THEN** the JSON contains `"strategy": "seasonAndEpisodeNumber"` (via JsonStringEnumConverter on the Messages enum)
