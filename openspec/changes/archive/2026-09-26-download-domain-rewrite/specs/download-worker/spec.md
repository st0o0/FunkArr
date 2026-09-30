# Download Worker (Delta)

## MODIFIED Requirements

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

### Requirement: DownloadWorker uses TimeProvider
The DownloadWorker SHALL use `TimeProvider` for all timestamp generation instead of `DateTimeOffset.UtcNow`.

#### Scenario: TimeProvider injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `TimeProvider` via constructor injection

#### Scenario: Completion timestamp
- **WHEN** a download completes or fails
- **THEN** the completion timestamp SHALL be generated via `_timeProvider.GetUtcNow().ToUnixTimeSeconds()`

### Requirement: DownloadWorker constructs RecordDownload via state extension
The DownloadWorker SHALL use a single `ToRecordDownload` extension method on `DownloadWorkerState` to construct `RecordDownload` messages, eliminating the current 3-site duplication.

#### Scenario: RecordDownload for success
- **WHEN** a download succeeds
- **THEN** `_state.ToRecordDownload(timeProvider, resolvedPaths)` SHALL produce a RecordDownload with status Completed, the relative path, and no fail message

#### Scenario: RecordDownload for failure
- **WHEN** a download fails
- **THEN** `_state.ToRecordDownload(timeProvider, null)` SHALL produce a RecordDownload with status Failed, null path, and the fail message

### Requirement: DownloadWorker state includes route info
The DownloadWorkerState SHALL include `RouteName` (string) and `ProxyUrl` (string?) fields that are populated from the `DownloadInitialized` persistence event and survive recovery.

#### Scenario: Route info persisted
- **WHEN** a DownloadInitialized event is applied
- **THEN** the state SHALL contain the RouteName and ProxyUrl from the event

#### Scenario: Route info survives recovery
- **WHEN** the Worker recovers from a crash
- **THEN** the RouteName and ProxyUrl SHALL be available from the recovered state

### Requirement: DownloadWorker recovery resets transient phases
The DownloadWorker SHALL reset to Initialized phase on recovery if the recovered phase is a transient in-flight phase.

#### Scenario: Recovery from Downloading
- **WHEN** the Worker recovers with phase VideoDownload, SubtitleDownload, Remuxing, or Moving
- **THEN** it SHALL reset phase to Initialized and wait for a StartDownload message

#### Scenario: Recovery from Completed
- **WHEN** the Worker recovers with phase Completed
- **THEN** it SHALL remain in Completed phase

#### Scenario: Recovery from Failed
- **WHEN** the Worker recovers with phase Failed
- **THEN** it SHALL remain in Failed phase

### Requirement: DownloadWorker implements IWithTimers
The DownloadWorker SHALL implement `IWithTimers` to support retry backoff scheduling.

#### Scenario: Timer available
- **WHEN** the Worker needs to schedule a retry
- **THEN** it SHALL use `Timers.StartSingleTimer` to schedule a `RetryAttempt` message
