# Download Messages (Delta)

## ADDED Requirements

### Requirement: FailureKind enum
The system SHALL define a `FailureKind` enum in `FunkArr.Messages.Download` with values `Transient` (0) and `Permanent` (1).

#### Scenario: Enum values
- **WHEN** the FailureKind enum is inspected
- **THEN** it SHALL contain `Transient` (0) and `Permanent` (1)

### Requirement: DownloadAttemptStarted persistence DTO
The system SHALL define a `DownloadAttemptStarted` persistence DTO in `FunkArr.Persistence.Events.Download` for tracking retry attempts.

#### Scenario: DownloadAttemptStarted fields
- **WHEN** a `DownloadAttemptStarted` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid) and `Attempt` (int)

### Requirement: DownloadPhaseChanged persistence DTO
The system SHALL define a `DownloadPhaseChanged` persistence DTO in `FunkArr.Persistence.Events.Download` for tracking phase transitions.

#### Scenario: DownloadPhaseChanged fields
- **WHEN** a `DownloadPhaseChanged` event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid) and `Phase` (int, mapped from DownloadPhase enum)

### Requirement: HistoryTrimmed persistence DTO
The system SHALL define a `HistoryTrimmed` persistence DTO in `FunkArr.Persistence.Events.Download` for recording history trim operations.

#### Scenario: HistoryTrimmed fields
- **WHEN** a `HistoryTrimmed` event is persisted
- **THEN** it SHALL contain `Count` (int) representing the number of records trimmed

### Requirement: PersistedFailureKind enum
The system SHALL define a `PersistedFailureKind` enum in `FunkArr.Persistence` with values `Transient` (0) and `Permanent` (1).

#### Scenario: Enum values
- **WHEN** the PersistedFailureKind enum is inspected
- **THEN** it SHALL contain `Transient` (0) and `Permanent` (1)

## MODIFIED Requirements

### Requirement: DownloadStarted persistence DTO
The system SHALL define a `DownloadInitialized` persistence DTO in `FunkArr.Persistence.Events.Download` for the Worker's initialization event. Infrastructure paths SHALL NOT be persisted. Route info SHALL be persisted.

#### Scenario: DownloadInitialized fields
- **WHEN** a DownloadInitialized event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (PersistedMediaType), `RouteName` (string, default "Direct"), `ProxyUrl` (string?, default null)
- **AND** it SHALL NOT contain `IncompletePath` or `OutputPath`

### Requirement: DownloadFailed persistence DTO
The system SHALL define a `DownloadFaulted` persistence DTO in `FunkArr.Persistence.Events.Download` for the Worker's failure event, including failure classification.

#### Scenario: DownloadFaulted DTO fields
- **WHEN** a DownloadFaulted persistence event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Reason` (string), and `FailureKind` (PersistedFailureKind, default Permanent)

### Requirement: WorkerStatusResult response
The system SHALL define a `WorkerStatusResult` record returned by the Worker containing full current state, live progress, phase, and attempt info.

#### Scenario: WorkerStatusResult fields
- **WHEN** a `WorkerStatusResult` message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (MediaType), `Size` (long), `Status` (WorkerStatus), `BytesDownloaded` (long), `CurrentTimeUs` (long), `TotalDuration` (int), `Speed` (double), `FailMessage` (string?), `Channel` (string), `HasSubtitles` (bool), `Phase` (DownloadPhase), `Attempt` (int)
- **AND** it SHALL NOT contain `FilePath`

### Requirement: FfmpegResult includes FailureKind
The system SHALL update the `FfmpegResult` record to include a `FailureKind` field.

#### Scenario: FfmpegResult fields
- **WHEN** an `FfmpegResult` is created
- **THEN** it SHALL contain `Success` (bool), `ExitCode` (int), `Error` (string?), `ElapsedSeconds` (int), `FailureKind` (FailureKind, default Permanent)
