# Download Worker

## Purpose

Sharded Entity actor that runs FFmpeg, parses progress, and manages per-download lifecycle.

## Requirements

### Requirement: DownloadWorker is a Sharded Entity
The DownloadWorker SHALL be registered as a Persistent Sharded Entity with the DownloadId (Guid) as the entity key. PersistenceId SHALL be `"download-{entityId}"`.

#### Scenario: Shard registration
- **WHEN** the actor system starts
- **THEN** a DownloadWorker shard region SHALL be registered with persistence enabled

### Requirement: DownloadWorker handles InitDownload
The DownloadWorker SHALL handle `InitDownload` messages by persisting all download metadata as a `DownloadInitialized` event and setting status to Initialized. Infrastructure paths are NOT part of the persisted metadata.

#### Scenario: First initialization
- **WHEN** an InitDownload message is received and the Worker has no persisted state
- **THEN** the Worker SHALL persist a DownloadInitialized event with domain metadata (Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category)
- **AND** set status to Initialized
- **AND** the event SHALL NOT contain IncompletePath or OutputPath

#### Scenario: Already initialized
- **WHEN** an InitDownload message is received but the Worker already has persisted state
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL compute paths from injected `DownloadOptions` and persisted metadata, ensure the incomplete directory exists, and spawn the FFmpeg process.

#### Scenario: Path computation at start
- **WHEN** a StartDownload is received and the Worker is Initialized
- **THEN** the Worker SHALL compute `IncompletePath` as `Path.Combine(downloadOptions.IncompletePath, entityId)`
- **AND** compute `OutputPath` using `downloadOptions.ResolveCategoryDir(category)` under `downloadOptions.CompletePath`, with title as subfolder, and `title + ".mkv"` as filename
- **AND** store computed paths as private fields (not persisted state)

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL ensure the computed `IncompletePath` directory exists
- **AND** persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with both video and subtitle inputs

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL ensure the computed `IncompletePath` directory exists
- **AND** persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with only the video input

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker handles CancelDownload
The DownloadWorker SHALL handle `CancelDownload` messages by cancelling the CancellationTokenSource, killing the FFmpeg process, and passivating.

#### Scenario: Cancel active download
- **WHEN** a CancelDownload message is received while FFmpeg is running
- **THEN** the Worker SHALL cancel the CancellationTokenSource
- **AND** the FFmpeg process SHALL be killed
- **AND** the Worker SHALL passivate

#### Scenario: Cancel idle worker
- **WHEN** a CancelDownload message is received while no FFmpeg process is running
- **THEN** the Worker SHALL passivate

### Requirement: DownloadWorker handles ResetDownload
The DownloadWorker SHALL handle `ResetDownload` messages by resetting its status to Initialized so it can be re-dispatched.

#### Scenario: Reset failed worker
- **WHEN** a ResetDownload message is received and the Worker's status is Failed
- **THEN** the Worker SHALL persist a `DownloadInitialized` event (re-using existing metadata, without paths) and set status to Initialized

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
- **THEN** the Worker SHALL persist a DownloadSucceeded event
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

### Requirement: DownloadWorker subtitle failure is non-fatal
The DownloadWorker SHALL proceed without subtitles if the subtitle URL fails to download or FFmpeg cannot convert the subtitle format.

#### Scenario: Subtitle download fails
- **WHEN** FFmpeg fails due to a subtitle input error
- **THEN** the Worker SHALL retry the FFmpeg command without the subtitle input using the same CancellationTokenSource

### Requirement: DownloadWorker cancellation
The DownloadWorker SHALL use a CancellationTokenSource tied to the actor lifecycle for FFmpeg process management.

#### Scenario: Actor stops while FFmpeg runs
- **WHEN** the Worker's PostStop is called while an FFmpeg process is running
- **THEN** the CancellationTokenSource SHALL be cancelled
- **AND** the FFmpeg process SHALL be killed and disposed

#### Scenario: Cancellation propagation to Task
- **WHEN** the CancellationTokenSource is cancelled
- **THEN** the background Task reading FFmpeg stdout SHALL observe cancellation and stop

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification and current status, but NOT infrastructure paths. Paths are computed at runtime.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
- **AND** it SHALL NOT contain IncompletePath or OutputPath

### Requirement: DownloadWorker cleans up incomplete directory
The DownloadWorker SHALL delete the `IncompletePath` directory after successful mux completion. Cleanup failure SHALL be logged but SHALL NOT affect download status.

#### Scenario: Successful cleanup
- **WHEN** FFmpeg exits with code 0 and the output file is moved to `OutputPath`
- **THEN** the Worker SHALL delete the `IncompletePath` directory recursively
- **AND** log a debug message on success

#### Scenario: Cleanup failure
- **WHEN** the `IncompletePath` directory cannot be deleted (e.g., file locked)
- **THEN** the Worker SHALL log a warning
- **AND** the download status SHALL still be Completed

#### Scenario: No cleanup on failure
- **WHEN** FFmpeg exits with a non-zero code
- **THEN** the Worker SHALL NOT delete the `IncompletePath` directory (files remain for debugging)

### Requirement: DownloadWorker ensures output directory exists
The DownloadWorker SHALL ensure the output directory (parent of `OutputPath`) exists before moving the finished file.

#### Scenario: Output directory creation
- **WHEN** FFmpeg completes successfully
- **THEN** the Worker SHALL call `Directory.CreateDirectory` on the output directory before writing the final `.mkv` file

### Requirement: DownloadWorker recovery
The DownloadWorker SHALL recover its full state from persisted events on restart.

#### Scenario: Recovery from Initialized
- **WHEN** the Worker recovers with status Initialized
- **THEN** it SHALL wait for a StartDownload message from the Manager

#### Scenario: Recovery from Downloading
- **WHEN** the Worker recovers with status Downloading (FFmpeg was running at crash time)
- **THEN** it SHALL reset status to Initialized and wait for a StartDownload message

#### Scenario: Recovery from Completed
- **WHEN** the Worker recovers with status Completed
- **THEN** it SHALL passivate immediately

#### Scenario: Recovery from Failed
- **WHEN** the Worker recovers with status Failed
- **THEN** it SHALL passivate immediately (Manager may send ResetDownload later for retry)

### Requirement: DownloadWorker receives DownloadOptions via DI
The DownloadWorker SHALL receive `IOptionsMonitor<DownloadOptions>` via constructor injection to compute paths at runtime.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IOptionsMonitor<DownloadOptions>` via its constructor
- **AND** use the current value to compute paths when handling StartDownload
