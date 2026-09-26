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
The DownloadWorker SHALL handle `InitDownload` messages by persisting all download metadata as a `DownloadInitialized` event and setting status to Initialized. The message SHALL include a route name and optional proxy URL resolved from the channel's route. Route info SHALL be persisted in the event.

#### Scenario: First initialization
- **WHEN** an InitDownload message is received and the Worker has no persisted state
- **THEN** the Worker SHALL persist a DownloadInitialized event with domain metadata (Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category, RouteName, ProxyUrl)
- **AND** set phase to Initialized
- **AND** set attempt count to 0

#### Scenario: Already initialized
- **WHEN** an InitDownload message is received but the Worker already has persisted state
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL persist a `DownloadAttemptStarted` event, delegate media remuxing to `IRemuxer.RunAsync` using persisted route info, and store the returned `CancellationTokenSource`.

#### Scenario: Start with proxy
- **WHEN** a StartDownload is received and the Worker has a non-null ProxyUrl and RouteName persisted in state
- **THEN** the Worker SHALL persist a `DownloadAttemptStarted` event with attempt number 1
- **AND** pass both RouteName and ProxyUrl through to `IRemuxer.RunAsync`

#### Scenario: Start without proxy
- **WHEN** a StartDownload is received and the Worker has RouteName "Direct" and null ProxyUrl in persisted state
- **THEN** the Worker SHALL persist a `DownloadAttemptStarted` event
- **AND** pass RouteName "Direct" and null ProxyUrl to `IRemuxer.RunAsync`

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL call `IRemuxer.RunAsync(videoUrl, subtitleUrl, outputPath, routeName, proxyUrl, onProgress, ct)`

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL call `IRemuxer.RunAsync(videoUrl, null, outputPath, routeName, proxyUrl, onProgress, ct)`

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message

#### Scenario: Start with empty video URL
- **WHEN** a StartDownload is received and the video URL is empty
- **THEN** the Worker SHALL persist a `DownloadFaulted` event with `FailureKind.Permanent`
- **AND** send `SlotFree` to the Manager
- **AND** send `RecordDownload` via state extension method
- **AND** NOT start FFmpeg

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

### Requirement: DownloadWorker constructs RecordDownload via state extension
The DownloadWorker SHALL use a single `ToRecordDownload` extension method on `DownloadWorkerState` to construct `RecordDownload` messages, eliminating the current 3-site duplication.

#### Scenario: RecordDownload for success
- **WHEN** a download succeeds
- **THEN** `_state.ToRecordDownload(timeProvider, resolvedPaths)` SHALL produce a RecordDownload with status Completed, the relative path, and no fail message

#### Scenario: RecordDownload for failure
- **WHEN** a download fails
- **THEN** `_state.ToRecordDownload(timeProvider, null)` SHALL produce a RecordDownload with status Failed, null path, and the fail message

### Requirement: DownloadWorker reports completion
The DownloadWorker SHALL receive `FfmpegResult` messages and handle success and failure cases. RecordDownload SHALL be constructed via a single state extension method.

#### Scenario: Successful completion
- **WHEN** an `FfmpegResult` is received with Success true
- **THEN** the Worker SHALL persist a `DownloadSucceeded` event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` (constructed via `_state.ToRecordDownload(...)`) to the HistoryManager

#### Scenario: FFmpeg failure
- **WHEN** an `FfmpegResult` is received with Success false
- **THEN** the Worker SHALL persist a `DownloadFaulted` event with the classified FailureKind
- **AND** handle retry or failure notification based on FailureKind and retry configuration

### Requirement: DownloadWorker cancellation
The DownloadWorker SHALL cancel the `CancellationTokenSource` returned by `FfmpegRunner.Run` to stop an active download.

#### Scenario: Cancel active download
- **WHEN** a CancelDownload message is received while a download is active
- **THEN** the Worker SHALL cancel and dispose the stored CancellationTokenSource
- **AND** passivate

#### Scenario: Actor stops while FFmpeg runs
- **WHEN** the Worker's PostStop is called while a CancellationTokenSource exists
- **THEN** the Worker SHALL cancel and dispose the CancellationTokenSource

### Requirement: DownloadWorker uses TimeProvider
The DownloadWorker SHALL use `TimeProvider` for all timestamp generation instead of `DateTimeOffset.UtcNow`.

#### Scenario: TimeProvider injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `TimeProvider` via constructor injection

#### Scenario: Completion timestamp
- **WHEN** a download completes or fails
- **THEN** the completion timestamp SHALL be generated via `_timeProvider.GetUtcNow().ToUnixTimeSeconds()`

### Requirement: DownloadWorker state includes route info
The DownloadWorkerState SHALL include `RouteName` (string) and `ProxyUrl` (string?) fields that are populated from the `DownloadInitialized` persistence event and survive recovery.

#### Scenario: Route info persisted
- **WHEN** a DownloadInitialized event is applied
- **THEN** the state SHALL contain the RouteName and ProxyUrl from the event

#### Scenario: Route info survives recovery
- **WHEN** the Worker recovers from a crash
- **THEN** the RouteName and ProxyUrl SHALL be available from the recovered state

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification and current status, but NOT infrastructure paths. A separate non-persisted field holds resolved download paths from `DataPaths.ResolveDownload()`.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), RouteName (string), ProxyUrl (string?), Phase (DownloadPhase), Attempt (int), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
- **AND** it SHALL NOT contain IncompletePath or OutputPath
- **AND** the Worker SHALL hold resolved paths separate from the state record

### Requirement: DownloadWorker recovery resets transient phases
The DownloadWorker SHALL recover its full state from persisted events on restart, recompute paths using `DataPaths.ResolveDownload()`, and reset to Initialized phase if the recovered phase is a transient in-flight phase.

#### Scenario: Recovery from Initialized
- **WHEN** the Worker recovers with status Initialized
- **THEN** it SHALL call `DataPaths.ResolveDownload()` to recompute paths and wait for a StartDownload message

#### Scenario: Recovery from Downloading
- **WHEN** the Worker recovers with phase VideoDownload, SubtitleDownload, Remuxing, or Moving
- **THEN** it SHALL reset phase to Initialized and wait for a StartDownload message

#### Scenario: Recovery from Completed
- **WHEN** the Worker recovers with phase Completed
- **THEN** it SHALL remain in Completed phase

#### Scenario: Recovery from Failed
- **WHEN** the Worker recovers with phase Failed
- **THEN** it SHALL remain in Failed phase

### Requirement: DownloadWorker receives IDataFiles and DataPaths via DI
The DownloadWorker SHALL receive `IRemuxer`, `IDataFiles`, `DataPaths`, `TimeProvider`, and `IOptions<DownloadOptions>` via constructor injection.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IRemuxer`, `IDataFiles`, `DataPaths`, `TimeProvider`, and `IOptions<DownloadOptions>` via its constructor
- **AND** use `DataPaths.ResolveDownload()` with the options categories for path resolution
- **AND** use `IDataFiles` for all filesystem operations

### Requirement: DownloadWorker implements IWithTimers
The DownloadWorker SHALL implement `IWithTimers` to support retry backoff scheduling.

#### Scenario: Timer available
- **WHEN** the Worker needs to schedule a retry
- **THEN** it SHALL use `Timers.StartSingleTimer` to schedule a `RetryAttempt` message

### Requirement: DownloadWorker Persist handlers separate state from side-effects
DownloadWorker SHALL use DeferAsync for handlers with multiple Tell targets
or external process launches. Clean state-only handlers SHALL remain unchanged.

#### Scenario: HandleStart with empty URL
- **WHEN** a download starts but has no URL
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL contain `_downloadManager.Tell` and `_downloadHistory.Tell`

#### Scenario: HandleStart with normal URL
- **WHEN** a download starts with a valid URL
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL contain `StartFfmpeg()` call

#### Scenario: HandleFfmpegResult success
- **WHEN** FFmpeg completes successfully
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL contain file cleanup, `_downloadManager.Tell`, and `_downloadHistory.Tell` (using `_state.ToRecordDownload(...)`)

#### Scenario: HandleFfmpegResult failure
- **WHEN** FFmpeg fails
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL handle retry scheduling or `_downloadManager.Tell` and `_downloadHistory.Tell` based on FailureKind and retry config

#### Scenario: HandleInit and HandleReset unchanged
- **WHEN** a download is initialized or reset
- **THEN** the existing state-only Persist callbacks SHALL remain unchanged

