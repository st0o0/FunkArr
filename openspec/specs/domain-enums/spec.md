## Purpose

Core enum types (`SearchSource`, `MediaType`) shared across all domain projects, with JSON serialization attributes for external format compatibility.

## Requirements

### Requirement: SearchSource enum
FunkArr.Core SHALL define a `SearchSource` enum identifying the caller that triggered a search. It SHALL use `JsonStringEnumConverter<SearchSource>` for string serialization compatibility with existing persisted events. Every member SHALL have an explicit int value assigned.

#### Scenario: SearchSource enum members
- **WHEN** the `SearchSource` enum is inspected
- **THEN** it SHALL contain members: `Sonarr`, `Radarr`, `Prowlarr`, `Ui`, `Test`
- **AND** each member SHALL have a `JsonStringEnumMemberName` attribute matching its lowercase form (`"sonarr"`, `"radarr"`, `"prowlarr"`, `"ui"`, `"test"`)
- **AND** each member SHALL have an explicit int value assigned

#### Scenario: SearchSource JSON roundtrip
- **WHEN** `SearchSource.Sonarr` is serialized to JSON
- **THEN** it SHALL produce `"sonarr"`
- **AND** deserializing `"sonarr"` SHALL produce `SearchSource.Sonarr`

### Requirement: MediaType enum in Core
FunkArr.Core SHALL define a `MediaType` enum distinguishing series from films. It SHALL use `JsonStringEnumConverter<MediaType>` with `JsonStringEnumMemberName` attributes matching the ruleset JSON format. Every member SHALL have an explicit int value assigned.

#### Scenario: MediaType enum members
- **WHEN** the `MediaType` enum is inspected
- **THEN** it SHALL contain members: `Show` (with `JsonStringEnumMemberName("show")`) and `Movie` (with `JsonStringEnumMemberName("movie")`)
- **AND** each member SHALL have an explicit int value assigned

#### Scenario: MediaType JSON roundtrip
- **WHEN** `MediaType.Show` is serialized to JSON
- **THEN** it SHALL produce `"show"`
- **AND** deserializing `"show"` SHALL produce `MediaType.Show`

#### Scenario: MediaType used across all domains
- **WHEN** any domain project needs to distinguish series from films
- **THEN** it SHALL use `MediaType` from `FunkArr.Core`, not a string literal

### Requirement: All enums have explicit int values
Every member of every enum in any FunkArr project SHALL have an explicit integer value assigned. Implicit ordinals SHALL NOT be used. This prevents silent value changes when members are reordered.

#### Scenario: Messages enum with explicit values
- **WHEN** any enum in `FunkArr.Messages` is inspected
- **THEN** every member SHALL have an explicit `= N` value

#### Scenario: Domain enum with explicit values
- **WHEN** any enum in a domain project (Search, Download, Feed, Core, etc.) is inspected
- **THEN** every member SHALL have an explicit `= N` value

### Requirement: Wire-boundary enums have Unknown = 0
Every enum in `FunkArr.Messages` and `FunkArr.Api` SHALL have `Unknown = 0` as its first member. There are no exemptions.

#### Scenario: Messages enum with Unknown
- **WHEN** `MediaType` in `FunkArr.Messages` is inspected
- **THEN** its first member SHALL be `Unknown = 0`
- **AND** existing members SHALL follow with explicit values starting from `1`

#### Scenario: DownloadPriority renumbered with Unknown
- **WHEN** `DownloadPriority` in `FunkArr.Messages` is inspected
- **THEN** members SHALL be `Unknown = 0, Paused = 1, Low = 2, Normal = 3, High = 4`

#### Scenario: DownloadStatus with Unknown
- **WHEN** `DownloadStatus` in `FunkArr.Messages` is inspected
- **THEN** its first member SHALL be `Unknown = 0`
- **AND** `Queued = 1, Processing = 2, Completed = 3, Failed = 4`

#### Scenario: DownloadPhase with Unknown
- **WHEN** `DownloadPhase` in `FunkArr.Messages` is inspected
- **THEN** its first member SHALL be `Unknown = 0`
- **AND** existing members SHALL follow with explicit values starting from `1`

#### Scenario: FailureKind with Unknown
- **WHEN** `FailureKind` in `FunkArr.Messages` is inspected
- **THEN** members SHALL be `Unknown = 0, Transient = 1, Permanent = 2`

#### Scenario: WorkerStatus with Unknown
- **WHEN** `WorkerStatus` in `FunkArr.Messages` is inspected
- **THEN** its first member SHALL be `Unknown = 0`
- **AND** existing members SHALL follow with explicit values starting from `1`

#### Scenario: FailureReason enums with Unknown
- **WHEN** `CreateLocalRuleSetFailureReason`, `UpdateLocalRuleSetFailureReason`, `DeleteLocalRuleSetFailureReason`, or `ExportRuleSetFailureReason` in `FunkArr.Messages` is inspected
- **THEN** each SHALL have `Unknown = 0` as its first member
- **AND** existing members SHALL follow with explicit values starting from `1`

#### Scenario: QueuePriority with Unknown
- **WHEN** `QueuePriority` in `FunkArr.Api` is inspected
- **THEN** members SHALL be `Unknown = 0, Paused = 1, Low = 2, Normal = 3, High = 4`

#### Scenario: Domain-internal enum exemption
- **WHEN** `SearchType` nested in `SearchManager` is inspected
- **THEN** it MAY omit `Unknown` (actor-internal, never serialized)
- **AND** each member SHALL still have an explicit int value
