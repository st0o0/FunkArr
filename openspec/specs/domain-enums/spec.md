## Purpose

Core enum types (`SearchSource`, `MediaType`) shared across all domain projects, with JSON serialization attributes for external format compatibility.

## Requirements

### Requirement: SearchSource enum
FunkArr.Core SHALL define a `SearchSource` enum identifying the caller that triggered a search. It SHALL use `JsonStringEnumConverter<SearchSource>` for string serialization compatibility with existing persisted events.

#### Scenario: SearchSource enum members
- **WHEN** the `SearchSource` enum is inspected
- **THEN** it SHALL contain members: `Sonarr`, `Radarr`, `Prowlarr`, `Ui`, `Test`
- **AND** each member SHALL have a `JsonStringEnumMemberName` attribute matching its lowercase form (`"sonarr"`, `"radarr"`, `"prowlarr"`, `"ui"`, `"test"`)

#### Scenario: SearchSource JSON roundtrip
- **WHEN** `SearchSource.Sonarr` is serialized to JSON
- **THEN** it SHALL produce `"sonarr"`
- **AND** deserializing `"sonarr"` SHALL produce `SearchSource.Sonarr`

### Requirement: MediaType enum in Core
FunkArr.Core SHALL define a `MediaType` enum distinguishing series from films. It SHALL use `JsonStringEnumConverter<MediaType>` with `JsonStringEnumMemberName` attributes matching the ruleset JSON format.

#### Scenario: MediaType enum members
- **WHEN** the `MediaType` enum is inspected
- **THEN** it SHALL contain members: `Show` (with `JsonStringEnumMemberName("show")`) and `Movie` (with `JsonStringEnumMemberName("movie")`)

#### Scenario: MediaType JSON roundtrip
- **WHEN** `MediaType.Show` is serialized to JSON
- **THEN** it SHALL produce `"show"`
- **AND** deserializing `"show"` SHALL produce `MediaType.Show`

#### Scenario: MediaType used across all domains
- **WHEN** any domain project needs to distinguish series from films
- **THEN** it SHALL use `MediaType` from `FunkArr.Core`, not a string literal
