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
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL delegate FFmpeg orchestration to `FfmpegRunner.Run` and store the returned `CancellationTokenSource`.

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event and call `FfmpegRunner.Run(Self, videoUrl, subtitleUrl, outputPath)`
- **AND** store the returned `CancellationTokenSource`

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event and call `FfmpegRunner.Run(Self, videoUrl, null, outputPath)`
- **AND** store the returned `CancellationTokenSource`

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker handles CancelDownload
The DownloadWorker SHALL handle `CancelDownload` messages by cancelling the stored CancellationTokenSource and passivating.

#### Scenario: Cancel active download
- **WHEN** a CancelDownload message is received while a download is active
- **THEN** the Worker SHALL cancel and dispose the stored CancellationTokenSource
- **AND** passivate

#### Scenario: Cancel idle worker
- **WHEN** a CancelDownload message is received while no download is active
- **THEN** the Worker SHALL passivate

### Requirement: DownloadWorker handles ResetDownload
The DownloadWorker SHALL handle `ResetDownload` messages by resetting its status to Initialized so it can be re-dispatched.

#### Scenario: Reset failed worker
- **WHEN** a ResetDownload message is received and the Worker's status is Failed
- **THEN** the Worker SHALL persist a `DownloadInitialized` event (re-using existing metadata, without paths) and set status to Initialized

### Requirement: DownloadWorker handles QueryWorkerStatus
The DownloadWorker SHALL handle `QueryWorkerStatus` messages by responding with a `WorkerStatusResult` containing its full current state and live progress data. `FilePath` is removed from the response.

#### Scenario: Query active worker
- **WHEN** a `QueryWorkerStatus` message is received and the Worker is in Downloading status
- **THEN** the Worker SHALL respond with a `WorkerStatusResult` containing DownloadId, Title, Category, Size, Status, and current progress (BytesDownloaded, CurrentTimeUs, TotalDuration, Speed)
- **AND** the response SHALL NOT contain FilePath

#### Scenario: Query initialized worker
- **WHEN** a `QueryWorkerStatus` message is received and the Worker is in Initialized status
- **THEN** the Worker SHALL respond with a `WorkerStatusResult` with zero progress values

#### Scenario: Query uninitialized worker
- **WHEN** a `QueryWorkerStatus` message is received and the Worker has no persisted state
- **THEN** the Worker SHALL not respond (let Ask timeout)

### Requirement: DownloadWorker holds progress in-memory
The DownloadWorker SHALL receive `ProgressUpdate` messages from `FfmpegRunner` and store the latest progress data in-memory as part of its actor state.

#### Scenario: Progress storage
- **WHEN** a `ProgressUpdate` message is received from the runner
- **THEN** the Worker SHALL update its in-memory progress fields (BytesDownloaded from TotalSize, CurrentTimeUs from OutTimeUs, Speed)
- **AND** the Worker SHALL NOT send progress messages to the Manager

### Requirement: DownloadWorker notifies HistoryActor on completion
The DownloadWorker SHALL send a `RecordDownload` message to the DownloadHistoryActor when a download completes or fails, with `RelativePath` instead of absolute `FilePath`.

#### Scenario: Successful completion notification
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL send `RecordDownload` with DownloadId, Title, Category, Size, Completed status, `RelativePath` (from `DownloadPaths`), DownloadTimeSeconds, and CompletedAt to the HistoryActor

#### Scenario: Failure notification
- **WHEN** the FFmpeg process exits with a non-zero code (and it's not a retriable subtitle error)
- **THEN** the Worker SHALL send `RecordDownload` with DownloadId, Title, Category, Size, Failed status, FailMessage, null RelativePath, and CompletedAt to the HistoryActor

### Requirement: DownloadWorker reports completion
The DownloadWorker SHALL receive `ProcessExited` messages from `FfmpegRunner` and handle success, subtitle-retry, and failure cases.

#### Scenario: Successful completion
- **WHEN** a `ProcessExited` message is received with ExitCode 0
- **THEN** the Worker SHALL persist a DownloadSucceeded event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` to the HistoryActor
- **AND** passivate

### Requirement: DownloadWorker reports failure
The DownloadWorker SHALL handle `ProcessExited` messages with non-zero exit codes by persisting failure or retrying without subtitles.

#### Scenario: FFmpeg failure
- **WHEN** a `ProcessExited` message is received with a non-zero ExitCode and it is not a subtitle error
- **THEN** the Worker SHALL persist a `DownloadFaulted` event with the ErrorOutput
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` to the HistoryActor
- **AND** passivate

### Requirement: DownloadWorker subtitle failure is non-fatal
The DownloadWorker SHALL proceed without subtitles if the FFmpeg process fails due to a subtitle input error.

#### Scenario: Subtitle download fails
- **WHEN** a `ProcessExited` message is received with a non-zero ExitCode and the error is a subtitle error and SubtitleUrl is not null
- **THEN** the Worker SHALL clear SubtitleUrl from state and call `FfmpegRunner.Run(Self, videoUrl, null, outputPath)` to retry without subtitles

### Requirement: DownloadWorker cancellation
The DownloadWorker SHALL cancel the `CancellationTokenSource` returned by `FfmpegRunner.Run` to stop an active download.

#### Scenario: Cancel active download
- **WHEN** a CancelDownload message is received while a download is active
- **THEN** the Worker SHALL cancel and dispose the stored CancellationTokenSource
- **AND** passivate

#### Scenario: Actor stops while FFmpeg runs
- **WHEN** the Worker's PostStop is called while a CancellationTokenSource exists
- **THEN** the Worker SHALL cancel and dispose the CancellationTokenSource

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification and current status, but NOT infrastructure paths. A separate non-persisted field holds resolved download paths from `DataPaths.ResolveDownload()`.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
- **AND** it SHALL NOT contain IncompletePath or OutputPath
- **AND** the Worker SHALL hold resolved paths separate from the state record

### Requirement: DownloadWorker recovery
The DownloadWorker SHALL recover its full state from persisted events on restart and recompute paths using `DataPaths.ResolveDownload()`.

#### Scenario: Recovery from Initialized
- **WHEN** the Worker recovers with status Initialized
- **THEN** it SHALL call `DataPaths.ResolveDownload()` to recompute paths and wait for a StartDownload message

#### Scenario: Recovery from Downloading
- **WHEN** the Worker recovers with status Downloading (FFmpeg was running at crash time)
- **THEN** it SHALL reset status to Initialized, recompute paths using `DataPaths.ResolveDownload()`, and wait for a StartDownload message

#### Scenario: Recovery from Completed
- **WHEN** the Worker recovers with status Completed
- **THEN** it SHALL passivate immediately

#### Scenario: Recovery from Failed
- **WHEN** the Worker recovers with status Failed
- **THEN** it SHALL passivate immediately

### Requirement: DownloadWorker receives IDataFiles and DataPaths via DI
The DownloadWorker SHALL receive `IFfmpegRunner`, `IDataFiles`, `DataPaths`, and `IOptions<DownloadOptions>` via constructor injection.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IFfmpegRunner`, `IDataFiles`, `DataPaths`, and `IOptions<DownloadOptions>` via its constructor
- **AND** use `DataPaths.ResolveDownload()` with the options categories for path resolution
- **AND** use `IDataFiles` for all filesystem operations
