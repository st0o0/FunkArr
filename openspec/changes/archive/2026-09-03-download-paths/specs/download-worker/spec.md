# Download Worker

## MODIFIED Requirements

### Requirement: DownloadWorker handles InitDownload
The DownloadWorker SHALL handle `InitDownload` messages by persisting all download metadata as a `DownloadInitialized` event and setting status to Initialized.

#### Scenario: First initialization
- **WHEN** an InitDownload message is received and the Worker has no persisted state
- **THEN** the Worker SHALL persist a DownloadInitialized event with all metadata (Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category, IncompletePath, OutputPath)
- **AND** set status to Initialized

#### Scenario: Already initialized
- **WHEN** an InitDownload message is received but the Worker already has persisted state
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL ensure the incomplete directory exists, use its persisted metadata to build FFmpeg arguments with the incomplete path for working files, and spawn the FFmpeg process with a CancellationTokenSource.

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL ensure `IncompletePath` directory exists
- **AND** persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with both video and subtitle inputs, writing to `IncompletePath`

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL ensure `IncompletePath` directory exists
- **AND** persist a `DownloadStarted` event, create a CancellationTokenSource, and spawn FFmpeg with only the video input, writing to `IncompletePath`

### Requirement: DownloadWorker state
The DownloadWorker SHALL maintain a persistent state record containing the full download specification, current status, and in-memory progress data.

#### Scenario: State structure
- **WHEN** the Worker state is inspected
- **THEN** it SHALL contain Title, VideoUrl, SubtitleUrl (nullable), Channel, Duration, Size, Category, IncompletePath, OutputPath, WorkerStatus (Initialized/Downloading/Completed/Failed), FailMessage (nullable), and in-memory progress fields (BytesDownloaded, CurrentTimeUs, Speed)

## ADDED Requirements

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
