## MODIFIED Requirements

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

### Requirement: DownloadWorker handles ResetDownload
The DownloadWorker SHALL handle `ResetDownload` messages by resetting its status to Initialized so it can be re-dispatched.

#### Scenario: Reset failed worker
- **WHEN** a ResetDownload message is received and the Worker's status is Failed
- **THEN** the Worker SHALL persist a `DownloadInitialized` event (re-using existing metadata, without paths) and set status to Initialized

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification and current status, but NOT infrastructure paths. Paths are computed at runtime.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)
- **AND** it SHALL NOT contain IncompletePath or OutputPath

### Requirement: DownloadWorker receives DownloadOptions via DI
The DownloadWorker SHALL receive `IOptionsMonitor<DownloadOptions>` via constructor injection to compute paths at runtime.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IOptionsMonitor<DownloadOptions>` via its constructor
- **AND** use the current value to compute paths when handling StartDownload
