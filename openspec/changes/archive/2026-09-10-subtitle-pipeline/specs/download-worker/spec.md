# Download Worker

## MODIFIED Requirements

### Requirement: DownloadWorker receives IDataFiles and DataPaths via DI
The DownloadWorker SHALL receive `IRemuxer`, `IDataFiles`, `DataPaths`, and `IOptions<DownloadOptions>` via constructor injection.

#### Scenario: DI injection
- **WHEN** the DownloadWorker is created
- **THEN** it SHALL receive `IRemuxer`, `IDataFiles`, `DataPaths`, and `IOptions<DownloadOptions>` via its constructor
- **AND** use `DataPaths.ResolveDownload()` with the options categories for path resolution
- **AND** use `IDataFiles` for all filesystem operations

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL delegate media remuxing to `IRemuxer.RunAsync` and store the returned `CancellationTokenSource`.

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event and call `IRemuxer.RunAsync(videoUrl, subtitleUrl, outputPath, onProgress, ct)`

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event and call `IRemuxer.RunAsync(videoUrl, null, outputPath, onProgress, ct)`

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message

## REMOVED Requirements

### Requirement: DownloadWorker subtitle failure is non-fatal
**Reason**: Subtitle retry logic is no longer needed in the actor. The `IRemuxer` handles subtitle preparation failures by proceeding without subtitles. The worker never sees subtitle-specific errors.
**Migration**: Remove `IsSubtitleError` check from `HandleFfmpegResult`. All FFmpeg failures are treated uniformly — if FFmpeg fails, the download fails.
