## MODIFIED Requirements

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL use `IDownloadFileService` to resolve paths, ensure the incomplete directory exists, and spawn the FFmpeg process.

#### Scenario: Path computation at start
- **WHEN** a StartDownload is received and the Worker is Initialized
- **THEN** the Worker SHALL call `IDownloadFileService.ResolveTempPath(entityId, title)` to get the temp file path
- **AND** call `IDownloadFileService.ResolveOutputPath(title, category)` to get the final output path
- **AND** store computed paths as private fields (not persisted state)

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL ensure the incomplete directory exists via the file service
- **AND** persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with both video and subtitle inputs

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL ensure the incomplete directory exists via the file service
- **AND** persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with only the video input

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker reports completion
The DownloadWorker SHALL persist a `DownloadSucceeded` event, use `IDownloadFileService` to move the file to its final location and clean up, and send `SlotFree` to the Manager when FFmpeg exits with code 0.

#### Scenario: Successful completion
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL call `IDownloadFileService.MoveToComplete(tempPath, outputPath)` to move the file
- **AND** call `IDownloadFileService.CleanupIncomplete(entityId)` to remove the working directory
- **AND** persist a DownloadSucceeded event
- **AND** send `SlotFree(DownloadId)` to the Manager
- **AND** send `RecordDownload` to the HistoryActor
- **AND** passivate

### Requirement: DownloadWorker ensures output directory exists
The DownloadWorker SHALL delegate output directory creation to `IDownloadFileService.MoveToComplete`, which handles directory creation internally.

#### Scenario: Output directory creation
- **WHEN** FFmpeg completes successfully
- **THEN** the Worker SHALL call `IDownloadFileService.MoveToComplete` which creates the output directory before moving the file

### Requirement: DownloadWorker cleans up incomplete directory
The DownloadWorker SHALL delegate cleanup to `IDownloadFileService.CleanupIncomplete` after successful completion.

#### Scenario: Successful cleanup
- **WHEN** FFmpeg exits with code 0 and the output file is moved
- **THEN** the Worker SHALL call `IDownloadFileService.CleanupIncomplete(entityId)`

#### Scenario: No cleanup on failure
- **WHEN** FFmpeg exits with a non-zero code
- **THEN** the Worker SHALL NOT call `CleanupIncomplete`

### Requirement: DownloadWorker receives IDownloadFileService via DI
The DownloadWorker SHALL receive `IDownloadFileService` via constructor injection to delegate all filesystem operations.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IDownloadFileService` via its constructor
- **AND** use it for all path resolution, file moves, and cleanup operations
- **AND** no longer receive `IOptionsMonitor<DownloadOptions>` directly (the file service encapsulates config access)

### Requirement: DownloadWorker recovery
The DownloadWorker SHALL recover its full state from persisted events on restart and recompute paths using the file service.

#### Scenario: Recovery from Initialized
- **WHEN** the Worker recovers with status Initialized
- **THEN** it SHALL recompute paths using `IDownloadFileService` and wait for a StartDownload message

#### Scenario: Recovery from Downloading
- **WHEN** the Worker recovers with status Downloading (FFmpeg was running at crash time)
- **THEN** it SHALL reset status to Initialized, recompute paths using `IDownloadFileService`, and wait for a StartDownload message

#### Scenario: Recovery from Completed
- **WHEN** the Worker recovers with status Completed
- **THEN** it SHALL passivate immediately

#### Scenario: Recovery from Failed
- **WHEN** the Worker recovers with status Failed
- **THEN** it SHALL passivate immediately
