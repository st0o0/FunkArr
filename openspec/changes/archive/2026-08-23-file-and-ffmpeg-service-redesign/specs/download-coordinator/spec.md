## MODIFIED Requirements

### Requirement: Stage machine flow
The DownloadCoordinator SHALL progress through stages: `Accepted → Fetching → AcquiringSubtitle → ConvertingSubtitle → Muxing → Done`. Each stage spawns a transient child worker. The coordinator SHALL NOT store file paths (`_tempPath`, `_outputDir`, `_videoPath`, `_subtitlePath`) in its state. It SHALL track `bool _hasSubtitle` instead of `string? _subtitlePath`. Worker command messages SHALL carry only identity and semantic data (nzoId, URLs, title), not directory paths.

#### Scenario: Full download pipeline
- **WHEN** `StartDownload(nzoId, videoUrl, subtitleUrl, title)` is received with a direct MP4 URL and subtitle URL
- **THEN** the coordinator SHALL progress: spawn Mp4DownloadWorker → spawn SubtitleDownloadWorker → spawn SubtitleConvertWorker → spawn RemuxWorker → persist JobCompleted

#### Scenario: No subtitle available
- **WHEN** no subtitle URL is provided and the source is not HLS
- **THEN** the coordinator SHALL skip AcquiringSubtitle and ConvertingSubtitle, proceeding directly to Muxing with `hasSubtitle: false`

#### Scenario: Subtitle acquired sets flag
- **WHEN** a worker responds with `SubtitleAcquired(nzoId, found: true)`
- **THEN** the coordinator SHALL set `_hasSubtitle = true` and proceed to ConvertingSubtitle

#### Scenario: Subtitle not found
- **WHEN** a worker responds with `SubtitleAcquired(nzoId, found: false)`
- **THEN** the coordinator SHALL set `_hasSubtitle = false` and proceed directly to Muxing

### Requirement: Event-sourced stage machine
The DownloadCoordinator SHALL persist stage transitions: `JobAccepted`, `StageEntered`, `JobCompleted`, `JobFailed`, `JobCancelled`. Recovery SHALL reconstruct the current stage and resume from there. The `JobAccepted` domain event SHALL NOT include `TempPath` or `OutputDir` fields. The persistence DTO (`DcJobAcceptedDto`) SHALL retain the `[JsonProperty("tmp")]` and `[JsonProperty("out")]` fields for backward compatibility but recovery SHALL ignore their values — paths SHALL come from `IFileService`.

#### Scenario: Recovery resumes from last stage
- **WHEN** a DownloadCoordinator entity recovers with events showing `StageEntered(Muxing)` and the persisted `JobAccepted` DTO contains old `TempPath`/`OutputDir` values
- **THEN** it SHALL resume from the Muxing stage using current paths from `IFileService`, ignoring the persisted path values

#### Scenario: New events written without paths
- **WHEN** a new `JobAccepted` event is persisted
- **THEN** the `DcJobAcceptedDto` SHALL write empty strings for `TempPath` and `OutputDir`

### Requirement: Five transient child worker types
The DownloadCoordinator SHALL spawn these workers as children: `Mp4DownloadWorker` (IFileService), `HlsDownloadWorker` (IFfmpegService), `SubtitleDownloadWorker` (IFileService), `SubtitleExtractWorker` (IFfmpegService), `SubtitleConvertWorker` (IFileService), `RemuxWorker` (IFfmpegService). Workers SHALL receive commands without directory path parameters.

#### Scenario: Worker lifecycle
- **WHEN** a worker completes its task
- **THEN** it SHALL tell the parent with a result message (carrying only nzoId, no paths) and stop itself

#### Scenario: Worker failure
- **WHEN** a worker throws an unhandled exception
- **THEN** it SHALL be stopped (Directive.Stop supervision) and the coordinator SHALL persist JobFailed
