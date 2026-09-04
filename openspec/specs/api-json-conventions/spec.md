## Purpose

JSON serialization conventions for the internal API, ensuring consistent casing and enum representation across all endpoints.

## Requirements

### Requirement: camelCase property naming
The system SHALL configure `JsonNamingPolicy.CamelCase` as the default JSON serialization policy via `ConfigureHttpJsonOptions`. All JSON responses SHALL use camelCase property names (e.g. `ruleSetId`, `totalCount`).

#### Scenario: Record properties serialize as camelCase
- **WHEN** a response containing a record with property `RuleSetId` is serialized
- **THEN** the JSON output contains the key `ruleSetId`

#### Scenario: ArrApi models with explicit JsonPropertyName are unaffected
- **WHEN** a SABnzbd response model with `[JsonPropertyName("noofslots_total")]` is serialized
- **THEN** the JSON output uses `noofslots_total`, not the camelCase version of the C# property name

### Requirement: Enum string serialization
The system SHALL configure `JsonStringEnumConverter` so that enum values serialize as their string names instead of integer values.

#### Scenario: Enum value serializes as string
- **WHEN** a response containing `RuleOutcome.Matched` is serialized
- **THEN** the JSON output contains `"matched"` (camelCase), not `0`

#### Scenario: Enum round-trip
- **WHEN** a JSON payload containing `"filterFailed"` is deserialized to `RuleOutcome`
- **THEN** the value is `RuleOutcome.FilterFailed`
