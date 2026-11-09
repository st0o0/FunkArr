## MODIFIED Requirements

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
