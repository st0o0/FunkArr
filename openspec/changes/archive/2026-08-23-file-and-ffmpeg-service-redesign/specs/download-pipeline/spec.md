## MODIFIED Requirements

### Requirement: Data flows forward through pipeline models
Worker command messages SHALL carry only identity and semantic data. `FetchVideo(NzoId, Url)` — no `TempPath`. `AcquireSubtitle(NzoId, SubtitleUrl, HlsManifestUrl)` — no `TempPath`. `ConvertSubtitle(NzoId)` — no `SubtitlePath`, no `TempPath`. `RemuxVideo(NzoId, Title, HasSubtitle)` — no `VideoPath`, `SubtitlePath`, or `OutputDir`. Workers SHALL resolve all paths internally via `IFileService` or `IFfmpegService`.

#### Scenario: FetchVideo carries no path data
- **WHEN** the coordinator sends `FetchVideo` to a download worker
- **THEN** the message SHALL contain only `NzoId` and `Url`

#### Scenario: RemuxVideo carries no path data
- **WHEN** the coordinator sends `RemuxVideo` to the remux worker
- **THEN** the message SHALL contain only `NzoId`, `Title`, and `HasSubtitle`

#### Scenario: Worker resolves paths from service
- **WHEN** `Mp4DownloadWorker` processes a `FetchVideo` command
- **THEN** it SHALL call `IFileService.SaveVideoAsync(cmd.NzoId, httpStream)` without needing any path parameters

### Requirement: Response messages carry identity only
Worker response messages SHALL carry only the nzoId and semantic results, not file paths. `VideoFetched(NzoId)`. `SubtitleAcquired(NzoId, bool Found)`. `SubtitleConverted(NzoId)`. `VideoRemuxed(NzoId)`. Paths are deterministic from nzoId via `IFileService` and do not need to travel through messages.

#### Scenario: VideoFetched without path
- **WHEN** a download worker completes successfully
- **THEN** it SHALL tell the coordinator `VideoFetched(nzoId)` without a file path

#### Scenario: SubtitleAcquired with boolean
- **WHEN** a subtitle worker completes
- **THEN** it SHALL tell the coordinator `SubtitleAcquired(nzoId, found)` where `found` indicates whether a subtitle was acquired

#### Scenario: VideoRemuxed without path
- **WHEN** the remux worker completes successfully
- **THEN** it SHALL tell the coordinator `VideoRemuxed(nzoId)` without an output path
- **AND** the coordinator SHALL resolve the output path via `IFileService.GetOutputPath(title)` for the completion notification

### Requirement: StartDownload message without paths
`StartDownload` SHALL carry only `NzoId`, `VideoUrl`, `SubtitleUrl`, and `Title`. It SHALL NOT carry `TempPath` or `OutputDir`.

#### Scenario: QueueCoordinator sends StartDownload
- **WHEN** `QueueCoordinator.TryStartNext()` starts a queued job
- **THEN** it SHALL send `StartDownload(nzoId, downloadUrl, subtitleUrl, title)` without `_tempPath` or `_downloadPath`
