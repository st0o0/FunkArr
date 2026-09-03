## MODIFIED Requirements

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

### Requirement: DownloadWorker holds progress in-memory
The DownloadWorker SHALL receive `ProgressUpdate` messages from `FfmpegRunner` and store the latest progress data in-memory as part of its actor state.

#### Scenario: Progress storage
- **WHEN** a `ProgressUpdate` message is received from the runner
- **THEN** the Worker SHALL update its in-memory progress fields (BytesDownloaded from TotalSize, CurrentTimeUs from OutTimeUs, Speed)
- **AND** the Worker SHALL NOT send progress messages to the Manager

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
