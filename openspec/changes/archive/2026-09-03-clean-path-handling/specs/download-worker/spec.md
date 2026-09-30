## MODIFIED Requirements

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL use `DownloadPaths.Compute()` to resolve paths and `IDownloadFileOperations` to ensure directories, then spawn the FFmpeg process.

#### Scenario: Path computation at start
- **WHEN** a StartDownload is received and the Worker is Initialized
- **THEN** the Worker SHALL call `DownloadPaths.Compute(entityId, title, category, options)` to get all paths
- **AND** call `IDownloadFileOperations.EnsureDirectory()` to create the incomplete directory
- **AND** store the `DownloadPaths` instance as a single private field (not persisted state)
- **AND** the Worker SHALL NOT call `Path.GetDirectoryName`, `Directory.CreateDirectory`, or any other filesystem API directly

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with both video and subtitle inputs

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with only the video input

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker reports completion
The DownloadWorker SHALL persist a `DownloadSucceeded` event (without FilePath), use `IDownloadFileOperations` to move the file and clean up, and send `SlotFree` to the Manager when FFmpeg exits with code 0. The `RecordDownload` message SHALL carry `RelativePath` instead of an absolute `FilePath`.

#### Scenario: Successful completion
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL call `IDownloadFileOperations.EnsureDirectory(Path.GetDirectoryName(paths.CompletePath))` to create the output directory
- **AND** call `IDownloadFileOperations.MoveFile(paths.IncompletePath, paths.CompletePath)` to move the file
- **AND** call `IDownloadFileOperations.DeleteDirectory(incompleteDir)` to clean up
- **AND** persist a `DownloadSucceeded` event (without FilePath)
- **AND** send `RecordDownload` with `RelativePath` (from `DownloadPaths.RelativePath`) to the HistoryActor
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** passivate

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

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification and current status, but NOT infrastructure paths. A separate non-persisted `DownloadPaths?` field holds computed paths.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
- **AND** it SHALL NOT contain IncompletePath or OutputPath
- **AND** the Worker SHALL hold a `DownloadPaths?` field separate from the state record

### Requirement: DownloadWorker receives IDownloadFileOperations and DownloadOptions via DI
The DownloadWorker SHALL receive `IFfmpegRunner`, `IDownloadFileOperations`, and `IOptions<DownloadOptions>` via constructor injection.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IFfmpegRunner`, `IDownloadFileOperations`, and `IOptions<DownloadOptions>` via its constructor
- **AND** use `DownloadPaths.Compute()` with the options for path resolution
- **AND** use `IDownloadFileOperations` for all filesystem operations

### Requirement: DownloadWorker recovery
The DownloadWorker SHALL recover its full state from persisted events on restart and recompute paths using `DownloadPaths.Compute()`.

#### Scenario: Recovery from Initialized
- **WHEN** the Worker recovers with status Initialized
- **THEN** it SHALL call `DownloadPaths.Compute()` to recompute paths and wait for a StartDownload message

#### Scenario: Recovery from Downloading
- **WHEN** the Worker recovers with status Downloading (FFmpeg was running at crash time)
- **THEN** it SHALL reset status to Initialized, recompute paths using `DownloadPaths.Compute()`, and wait for a StartDownload message

#### Scenario: Recovery from Completed
- **WHEN** the Worker recovers with status Completed
- **THEN** it SHALL passivate immediately

#### Scenario: Recovery from Failed
- **WHEN** the Worker recovers with status Failed
- **THEN** it SHALL passivate immediately

### Requirement: DownloadWorker notifies HistoryActor on completion
The DownloadWorker SHALL send a `RecordDownload` message to the DownloadHistoryActor when a download completes or fails, with `RelativePath` instead of absolute `FilePath`.

#### Scenario: Successful completion notification
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL send `RecordDownload` with DownloadId, Title, Category, Size, Completed status, `RelativePath` (from `DownloadPaths`), DownloadTimeSeconds, and CompletedAt to the HistoryActor

#### Scenario: Failure notification
- **WHEN** the FFmpeg process exits with a non-zero code (and it's not a retriable subtitle error)
- **THEN** the Worker SHALL send `RecordDownload` with DownloadId, Title, Category, Size, Failed status, FailMessage, null RelativePath, and CompletedAt to the HistoryActor
