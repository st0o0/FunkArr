## ADDED Requirements

### Requirement: InitDownload command
The system SHALL define an `InitDownload` record sent from Manager to Worker to initialize the Worker with all download metadata.

#### Scenario: InitDownload fields
- **WHEN** an InitDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (string), `OutputPath` (string)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: CancelDownload command
The system SHALL define a `CancelDownload` record sent from Manager to Worker to cancel an active download and passivate the Worker.

#### Scenario: CancelDownload fields
- **WHEN** a CancelDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: ResetDownload command
The system SHALL define a `ResetDownload` record sent from Manager to Worker to reset a Failed Worker back to Initialized for retry.

#### Scenario: ResetDownload fields
- **WHEN** a ResetDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid)
- **AND** it SHALL implement `IWithDownloadId`

### Requirement: DownloadInitialized persistence DTO
The system SHALL define a `DownloadInitialized` persistence DTO for the Worker's initial metadata event.

#### Scenario: DownloadInitialized fields
- **WHEN** a DownloadInitialized event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `VideoUrl` (string), `SubtitleUrl` (string?), `Channel` (string), `Duration` (int), `Size` (long), `Category` (string), `OutputPath` (string)

### Requirement: DownloadStarted persistence DTO
The system SHALL define a `DownloadStarted` persistence DTO for the Worker's FFmpeg start event.

#### Scenario: DownloadStarted fields
- **WHEN** a DownloadStarted event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid)

### Requirement: DownloadSucceeded persistence DTO
The system SHALL define a `DownloadSucceeded` persistence DTO for the Worker's successful completion event.

#### Scenario: DownloadSucceeded fields
- **WHEN** a DownloadSucceeded event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `FilePath` (string), `DownloadTimeSeconds` (int), `CompletedAt` (long, Unix timestamp)

### Requirement: DownloadFailed persistence DTO
The system SHALL define a `DownloadFailed` persistence DTO in `FunkArr.Persistence.Events.Download` for the Worker's failure event. This is distinct from the `DownloadFailed` message in `FunkArr.Messages.Download`.

#### Scenario: DownloadFailed DTO fields
- **WHEN** a DownloadFailed persistence event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Reason` (string)

### Requirement: DownloadRegistered persistence DTO
The system SHALL define a `DownloadRegistered` persistence DTO for the Manager's initial registration event, replacing `DownloadQueued`.

#### Scenario: DownloadRegistered fields
- **WHEN** a DownloadRegistered event is persisted
- **THEN** it SHALL contain `DownloadId` (Guid), `Title` (string), `Category` (string), `Size` (long)

## MODIFIED Requirements

### Requirement: StartDownload command
The system SHALL define a `StartDownload` record as a bare go-signal from Manager to Worker containing only the DownloadId.

#### Scenario: StartDownload fields
- **WHEN** a StartDownload message is created
- **THEN** it SHALL contain `DownloadId` (Guid) only
- **AND** it SHALL implement `IWithDownloadId`

## REMOVED Requirements

### Requirement: DownloadStatus enum
**Reason**: The `Extracting`, `Moving`, `Verifying` values are unused in the redesigned flow. The enum is simplified to `Queued`, `Processing`, `Completed`, `Failed`.
**Migration**: Remove unused enum values. All existing code references only Queued, Processing, Completed, and Failed.

### Requirement: AddDownload command
**Reason**: The `Priority` field added in the current spec is a non-goal for this change. The AddDownload record retains its original fields without Priority.
**Migration**: AddDownload keeps Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category. Priority is removed (future work).
