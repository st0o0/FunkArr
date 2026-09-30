## MODIFIED Requirements

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL use `DataPaths.ResolveDownload()` to resolve paths and `IDataFiles` to ensure directories, then spawn the FFmpeg process.

#### Scenario: Path computation at start
- **WHEN** a StartDownload is received and the Worker is Initialized
- **THEN** the Worker SHALL call `DataPaths.ResolveDownload(entityId, title, category, options.Categories)` to get all paths
- **AND** call `IDataFiles.CreateDirectory()` to create the incomplete directory
- **AND** store the resolved paths as a private field (not persisted state)
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
The DownloadWorker SHALL persist a `DownloadSucceeded` event (without FilePath), use `IDataFiles` to move the file and clean up, and send `SlotFree` to the Manager when FFmpeg exits with code 0. The `RecordDownload` message SHALL carry `RelativePath` instead of an absolute `FilePath`.

#### Scenario: Successful completion
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL call `IDataFiles.CreateDirectory(Path.GetDirectoryName(paths.CompletePath))` to create the output directory
- **AND** call `IDataFiles.Move(paths.IncompletePath, paths.CompletePath)` to move the file
- **AND** call `IDataFiles.Remove(incompleteDir)` to clean up
- **AND** persist a `DownloadSucceeded` event (without FilePath)
- **AND** send `RecordDownload` with `RelativePath` to the HistoryActor
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** passivate

### Requirement: DownloadWorker receives IDataFiles and DataPaths via DI
The DownloadWorker SHALL receive `IFfmpegRunner`, `IDataFiles`, `DataPaths`, and `IOptions<DownloadOptions>` via constructor injection.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IFfmpegRunner`, `IDataFiles`, `DataPaths`, and `IOptions<DownloadOptions>` via its constructor
- **AND** use `DataPaths.ResolveDownload()` with the options categories for path resolution
- **AND** use `IDataFiles` for all filesystem operations

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

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification and current status, but NOT infrastructure paths. A separate non-persisted field holds resolved download paths from `DataPaths.ResolveDownload()`.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
- **AND** it SHALL NOT contain IncompletePath or OutputPath
- **AND** the Worker SHALL hold resolved paths separate from the state record
