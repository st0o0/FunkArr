## ADDED Requirements

### Requirement: DownloadWorker handles QueryWorkerStatus
The DownloadWorker SHALL handle `QueryWorkerStatus` messages by responding with a `WorkerStatusResult` containing its full current state and live progress data.

#### Scenario: Query active worker
- **WHEN** a `QueryWorkerStatus` message is received and the Worker is in Downloading status
- **THEN** the Worker SHALL respond with a `WorkerStatusResult` containing DownloadId, Title, Category, Size, Status, and current progress (BytesDownloaded, CurrentTimeUs, TotalDuration, Speed)

#### Scenario: Query initialized worker
- **WHEN** a `QueryWorkerStatus` message is received and the Worker is in Initialized status
- **THEN** the Worker SHALL respond with a `WorkerStatusResult` with zero progress values

#### Scenario: Query uninitialized worker
- **WHEN** a `QueryWorkerStatus` message is received and the Worker has no persisted state
- **THEN** the Worker SHALL not respond (let Ask timeout)

### Requirement: DownloadWorker holds progress in-memory
The DownloadWorker SHALL store its latest FFmpeg progress data in-memory as part of its actor state, replacing the previous push model to the Manager.

#### Scenario: Progress storage
- **WHEN** FFmpeg emits a progress block
- **THEN** the Worker SHALL update its in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
- **AND** the Worker SHALL NOT send progress messages to the Manager

### Requirement: DownloadWorker notifies HistoryActor on completion
The DownloadWorker SHALL send a `RecordDownload` message to the DownloadHistoryActor when a download completes or fails, in addition to notifying the Manager.

#### Scenario: Successful completion notification
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL persist a `DownloadSucceeded` event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` with DownloadId, Title, Category, Size, Completed status, FilePath, DownloadTimeSeconds, and CompletedAt to the HistoryActor
- **AND** passivate

#### Scenario: Failure notification
- **WHEN** the FFmpeg process exits with a non-zero code (and it's not a retriable subtitle error)
- **THEN** the Worker SHALL persist a `DownloadFaulted` event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` with DownloadId, Title, Category, Size, Failed status, FailMessage, and CompletedAt to the HistoryActor
- **AND** passivate

#### Scenario: FFmpeg process crash notification
- **WHEN** the FFmpeg process cannot be started
- **THEN** the Worker SHALL persist a `DownloadFaulted` event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` with Failed status and error message to the HistoryActor
- **AND** passivate

## MODIFIED Requirements

### Requirement: DownloadWorker reports progress
The DownloadWorker SHALL parse FFmpeg's `-progress pipe:1` output and store progress in-memory. Progress SHALL NOT be pushed to the Manager.

#### Scenario: Progress reporting
- **WHEN** FFmpeg emits a progress block containing `out_time_us`, `total_size`, and `speed`
- **THEN** the Worker SHALL update its in-memory progress state with the parsed values

#### Scenario: Progress interval
- **WHEN** FFmpeg emits progress blocks
- **THEN** the Worker SHALL update in-memory progress on every complete block (throttling is no longer needed since there is no message send)

### Requirement: DownloadWorker reports completion
The DownloadWorker SHALL persist a `DownloadSucceeded` event and send `SlotFree` to the Manager when FFmpeg exits with code 0.

#### Scenario: Successful completion
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL persist a `DownloadSucceeded` event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` to the HistoryActor
- **AND** passivate

### Requirement: DownloadWorker reports failure
The DownloadWorker SHALL persist a `DownloadFaulted` event and send `SlotFree` to the Manager when FFmpeg exits with a non-zero code.

#### Scenario: FFmpeg failure
- **WHEN** the FFmpeg process exits with a non-zero code (and it's not a retriable subtitle error)
- **THEN** the Worker SHALL persist a `DownloadFaulted` event with the stderr output
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` to the HistoryActor
- **AND** passivate

#### Scenario: FFmpeg process crash
- **WHEN** the FFmpeg process cannot be started
- **THEN** the Worker SHALL persist a `DownloadFaulted` event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` to the HistoryActor with an appropriate error message
- **AND** passivate

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification, current status, and in-memory progress data.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, OutputPath, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
