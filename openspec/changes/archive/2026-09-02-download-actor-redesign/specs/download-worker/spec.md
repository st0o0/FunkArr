## MODIFIED Requirements

### Requirement: DownloadWorker is a Sharded Entity
The DownloadWorker SHALL be registered as a Persistent Sharded Entity with the DownloadId (Guid) as the entity key. PersistenceId SHALL be `"download-{entityId}"`.

#### Scenario: Shard registration
- **WHEN** the actor system starts
- **THEN** a DownloadWorker shard region SHALL be registered with persistence enabled

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL use its persisted metadata to build FFmpeg arguments and spawn the FFmpeg process with a CancellationTokenSource.

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with both video and subtitle inputs

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with only the video input

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker reports progress
The DownloadWorker SHALL parse FFmpeg's `-progress pipe:1` output and send `DownloadProgress` messages to the DownloadManager at regular intervals.

#### Scenario: Progress reporting
- **WHEN** FFmpeg emits a progress block containing `out_time_us`, `total_size`, and `speed`
- **THEN** the Worker SHALL send a DownloadProgress message to the Manager with the parsed values

#### Scenario: Progress interval
- **WHEN** FFmpeg emits progress blocks
- **THEN** the Worker SHALL forward progress to the Manager no more frequently than once per second

### Requirement: DownloadWorker reports completion
The DownloadWorker SHALL persist a `DownloadSucceeded` event and send `DownloadCompleted` to the Manager when FFmpeg exits with code 0.

#### Scenario: Successful completion
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the Worker SHALL persist a DownloadSucceeded event
- **AND** send DownloadCompleted with the output file path and elapsed download time to the Manager
- **AND** passivate

### Requirement: DownloadWorker reports failure
The DownloadWorker SHALL persist a `DownloadFailed` event and send `DownloadFailed` to the Manager when FFmpeg exits with a non-zero code.

#### Scenario: FFmpeg failure
- **WHEN** the FFmpeg process exits with a non-zero code (and it's not a retriable subtitle error)
- **THEN** the Worker SHALL persist a DownloadFailed event with the stderr output
- **AND** send DownloadFailed to the Manager
- **AND** passivate

#### Scenario: FFmpeg process crash
- **WHEN** the FFmpeg process cannot be started
- **THEN** the Worker SHALL persist a DownloadFailed event
- **AND** send DownloadFailed to the Manager with an appropriate error message
- **AND** passivate

### Requirement: DownloadWorker subtitle failure is non-fatal
The DownloadWorker SHALL proceed without subtitles if the subtitle URL fails to download or FFmpeg cannot convert the subtitle format.

#### Scenario: Subtitle download fails
- **WHEN** FFmpeg fails due to a subtitle input error
- **THEN** the Worker SHALL retry the FFmpeg command without the subtitle input using the same CancellationTokenSource

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification and current status.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, OutputPath, WorkerStatus (Initialized/Downloading/Completed/Failed), and FailMessage (nullable)

## ADDED Requirements

### Requirement: DownloadWorker handles InitDownload
The DownloadWorker SHALL handle `InitDownload` messages by persisting all download metadata as a `DownloadInitialized` event and setting status to Initialized.

#### Scenario: First initialization
- **WHEN** an InitDownload message is received and the Worker has no persisted state
- **THEN** the Worker SHALL persist a DownloadInitialized event with all metadata (Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category, OutputPath)
- **AND** set status to Initialized

#### Scenario: Already initialized
- **WHEN** an InitDownload message is received but the Worker already has persisted state
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
- **THEN** the Worker SHALL persist a `DownloadInitialized` event (re-using existing metadata) and set status to Initialized

### Requirement: DownloadWorker cancellation
The DownloadWorker SHALL use a CancellationTokenSource tied to the actor lifecycle for FFmpeg process management.

#### Scenario: Actor stops while FFmpeg runs
- **WHEN** the Worker's PostStop is called while an FFmpeg process is running
- **THEN** the CancellationTokenSource SHALL be cancelled
- **AND** the FFmpeg process SHALL be killed and disposed

#### Scenario: Cancellation propagation to Task
- **WHEN** the CancellationTokenSource is cancelled
- **THEN** the background Task reading FFmpeg stdout SHALL observe cancellation and stop

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
