## MODIFIED Requirements

### Requirement: MediaType enum
The API SHALL use the shared `MediaType` enum from `FunkArr.Core` instead of defining its own. `RuleSetListEntry.MediaType` SHALL use the shared enum. The API layer's `JsonSerializerOptions` SHALL override the type-level `JsonStringEnumConverter` to serialize as integer in API responses, preserving the existing API contract.

#### Scenario: Show media type
- **WHEN** the API returns a ruleset with media type show
- **THEN** the JSON field `mediaType` SHALL be `0`

#### Scenario: MediaType enum comes from Core
- **WHEN** the API layer references `MediaType`
- **THEN** it SHALL use `FunkArr.Core.MediaType`, not a local definition

#### Scenario: RuleSetMappingExtensions simplified
- **WHEN** a ruleset's `media.type` string is mapped to `MediaType`
- **THEN** the JSON deserialization SHALL handle `"show"` -> `MediaType.Show` and `"movie"` -> `MediaType.Movie` automatically via `JsonStringEnumConverter`
- **AND** the manual string switch in `RuleSetMappingExtensions` SHALL be removed
