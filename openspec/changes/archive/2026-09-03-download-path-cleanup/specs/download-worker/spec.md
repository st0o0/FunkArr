## MODIFIED Requirements

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL use `IDownloadFileService` to resolve and ensure paths, then spawn the FFmpeg process. No inline `Path.*` or `Directory.*` calls.

#### Scenario: Path computation at start
- **WHEN** a StartDownload is received and the Worker is Initialized
- **THEN** the Worker SHALL call `IDownloadFileService.EnsureIncompletePath(entityId, title)` to get the temp file path (directory created by the service)
- **AND** call `IDownloadFileService.ResolveOutputPath(entityId, title, category)` to get the final output path
- **AND** store computed paths as private fields (not persisted state)
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

### Requirement: DownloadWorker recovery
The DownloadWorker SHALL recover its full state from persisted events on restart and recompute paths using the file service. No inline filesystem calls during recovery.

#### Scenario: Recovery from Initialized
- **WHEN** the Worker recovers with status Initialized
- **THEN** it SHALL call `IDownloadFileService.EnsureIncompletePath` and `IDownloadFileService.ResolveOutputPath` to recompute paths and wait for a StartDownload message

#### Scenario: Recovery from Downloading
- **WHEN** the Worker recovers with status Downloading (FFmpeg was running at crash time)
- **THEN** it SHALL reset status to Initialized, recompute paths using the file service, and wait for a StartDownload message

#### Scenario: Recovery from Completed
- **WHEN** the Worker recovers with status Completed
- **THEN** it SHALL passivate immediately

#### Scenario: Recovery from Failed
- **WHEN** the Worker recovers with status Failed
- **THEN** it SHALL passivate immediately
