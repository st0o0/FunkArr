# Download Worker Phases

## Purpose

Explicit phase state machine in the DownloadWorker replacing implicit status guards. Phases represent where in the download pipeline the Worker currently is.

## Requirements

### Requirement: DownloadWorker tracks explicit download phase
The DownloadWorkerState SHALL contain a `Phase` field of type `DownloadPhase` that represents the current pipeline position. Phase transitions SHALL be persisted.

#### Scenario: Phase after initialization
- **WHEN** a download is initialized via InitDownload
- **THEN** the phase SHALL be `DownloadPhase.Initialized`

#### Scenario: Phase during subtitle download
- **WHEN** FFmpeg begins and progress indicates subtitle preparation
- **THEN** the phase SHALL be `DownloadPhase.SubtitleDownload`

#### Scenario: Phase during video download
- **WHEN** FFmpeg progress shows bytes increasing but out_time_us is 0
- **THEN** the phase SHALL be `DownloadPhase.VideoDownload`

#### Scenario: Phase during remuxing
- **WHEN** FFmpeg progress shows bytes at total and out_time_us > 0
- **THEN** the phase SHALL be `DownloadPhase.Remuxing`

#### Scenario: Phase during file move
- **WHEN** FFmpeg completes successfully and the file is being moved from incomplete to complete path
- **THEN** the phase SHALL be `DownloadPhase.Moving`

#### Scenario: Phase after success
- **WHEN** the download completes successfully
- **THEN** the phase SHALL be `DownloadPhase.Completed`

#### Scenario: Phase after failure
- **WHEN** the download fails
- **THEN** the phase SHALL be `DownloadPhase.Failed`

### Requirement: DownloadPhase enum values
The `DownloadPhase` enum in `FunkArr.Messages.Download` SHALL be extended with additional values for the full pipeline.

#### Scenario: Enum values
- **WHEN** the DownloadPhase enum is inspected
- **THEN** it SHALL contain `Initialized` (0), `SubtitleDownload` (1), `VideoDownload` (2), `Remuxing` (3), `Moving` (4), `Completed` (5), `Failed` (6)

### Requirement: Phase transitions persisted via DownloadPhaseChanged
The DownloadWorker SHALL persist a `DownloadPhaseChanged` event on phase transitions except for transient in-flight phase changes derived from progress data.

#### Scenario: Phase persisted on start
- **WHEN** a download starts
- **THEN** a `DownloadPhaseChanged(DownloadId, DownloadPhase.VideoDownload)` event SHALL be persisted

#### Scenario: Phase inferred from progress (not persisted)
- **WHEN** progress data indicates a transition from VideoDownload to Remuxing
- **THEN** the in-memory phase SHALL update
- **AND** no persistence event SHALL be written for this transition

#### Scenario: Phase persisted on completion
- **WHEN** a download succeeds
- **THEN** the phase is implicitly captured by the `DownloadSucceeded` event (no separate PhaseChanged needed)

### Requirement: WorkerStatusResult includes phase
The `WorkerStatusResult` response SHALL include the current `DownloadPhase` so consumers do not need to derive it from progress data.

#### Scenario: Status query returns phase
- **WHEN** a `QueryWorkerStatus` is handled
- **THEN** the `WorkerStatusResult` SHALL include the current `Phase` value

### Requirement: Phase recovery from persistence
The DownloadWorker SHALL recover phase from persisted events. If the recovered phase is a transient phase (SubtitleDownload, VideoDownload, Remuxing, Moving), the Worker SHALL reset to Initialized.

#### Scenario: Recovery from VideoDownload phase
- **WHEN** the Worker recovers with phase VideoDownload
- **THEN** the phase SHALL be reset to Initialized

#### Scenario: Recovery from Completed phase
- **WHEN** the Worker recovers with phase Completed
- **THEN** the phase SHALL remain Completed
