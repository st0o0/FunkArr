# Capability: Download Coordinator

## Purpose

Orchestrates the lifecycle of a single download job as a ShardRegion entity. Manages an event-sourced stage machine that progresses through fetching, subtitle acquisition, subtitle conversion, and muxing stages via transient child workers, with failure classification and status reporting to the broader download pipeline.

## Requirements

### Requirement: DownloadActor ShardRegion entity
The system SHALL register a `DownloadActor` ShardRegion using `ShardedMessageExtractor` with `maxNumberOfShards: 10`. Each entity SHALL be identified by nzoId via `IWithNzoId` which extends `IShardedMessage`. The dedicated `DownloadActorMessageExtractor` class SHALL be removed. The namespace SHALL be `FunkArr.DownloadClient.Pipeline`. Each entity SHALL be a `ReceivePersistentActor` with `PersistenceId: "download-{nzoId}"`.

#### Scenario: Shard region uses unified extractor
- **WHEN** the application starts and registers the DownloadActor ShardRegion
- **THEN** it SHALL use `new ShardedMessageExtractor(10)` for entity ID extraction

#### Scenario: Messages route via IShardedMessage
- **WHEN** a `StartDownload` message with `NzoId = "dl-001"` is sent to the ShardRegion
- **THEN** `IShardedMessage.EntityKey` SHALL return `"dl-001"` (delegating to `IWithNzoId.NzoId`)

### Requirement: Event-sourced stage machine
The DownloadActor SHALL persist stage transitions: `JobAccepted`, `StageEntered`, `JobCompleted`, `JobFailed`, `JobCancelled`. Recovery SHALL reconstruct the current stage and resume from there. The `JobAccepted` domain event SHALL NOT include `TempPath` or `OutputDir` fields. The persistence DTO (`DcJobAcceptedDto`) SHALL retain the `[JsonProperty("tmp")]` and `[JsonProperty("out")]` fields for backward compatibility but recovery SHALL ignore their values — paths SHALL come from `IFileService`.

#### Scenario: Recovery resumes from last stage
- **WHEN** a DownloadActor entity recovers with events showing `StageEntered(Muxing)` and the persisted `JobAccepted` DTO contains old `TempPath`/`OutputDir` values
- **THEN** it SHALL resume from the Muxing stage using current paths from `IFileService`, ignoring the persisted path values

#### Scenario: New events written without paths
- **WHEN** a new `JobAccepted` event is persisted
- **THEN** the `DcJobAcceptedDto` SHALL write empty strings for `TempPath` and `OutputDir`

### Requirement: Stage machine flow
The DownloadActor SHALL progress through stages: `Accepted → Fetching → AcquiringSubtitle → ConvertingSubtitle → Muxing → Done`. Each stage spawns a transient child worker. The `StartDownload` message SHALL include an optional `Category` field alongside `NzoId`, `VideoUrl`, `SubtitleUrl`, and `Title`. The coordinator SHALL NOT store file paths (`_tempPath`, `_outputDir`, `_videoPath`, `_subtitlePath`) in its state. It SHALL track `bool _hasSubtitle` instead of `string? _subtitlePath`. Worker command messages SHALL carry only identity and semantic data (nzoId, URLs, title), not directory paths.

#### Scenario: Full download pipeline with category
- **WHEN** `StartDownload` is received with a direct MP4 URL, subtitle URL, and category `"tv"`
- **THEN** the coordinator SHALL store category, progress through all stages, and pass category to `RemuxVideo` so `FfmpegService` can resolve the output path via `FileService`

#### Scenario: Full download pipeline
- **WHEN** `StartDownload(nzoId, videoUrl, subtitleUrl, title)` is received with a direct MP4 URL and subtitle URL
- **THEN** the coordinator SHALL progress: spawn Mp4DownloadActor → spawn SubtitleDownloadActor → spawn SubtitleConvertActor → spawn RemuxActor → persist JobCompleted

#### Scenario: No subtitle available
- **WHEN** no subtitle URL is provided and the source is not HLS
- **THEN** the coordinator SHALL skip AcquiringSubtitle and ConvertingSubtitle, proceeding directly to Muxing with `hasSubtitle: false`

#### Scenario: Subtitle acquired sets flag
- **WHEN** a worker responds with `SubtitleAcquired(nzoId, found: true)`
- **THEN** the coordinator SHALL set `_hasSubtitle = true` and proceed to ConvertingSubtitle

#### Scenario: Subtitle not found
- **WHEN** a worker responds with `SubtitleAcquired(nzoId, found: false)`
- **THEN** the coordinator SHALL set `_hasSubtitle = false` and proceed directly to Muxing

### Requirement: Five transient child worker types
The DownloadActor SHALL spawn these workers as children: `Mp4DownloadActor` (IFileService), `HlsDownloadActor` (IFfmpegService), `SubtitleDownloadActor` (IFileService), `SubtitleExtractActor` (IFfmpegService), `SubtitleConvertActor` (IFileService), `RemuxActor` (IFfmpegService). Workers SHALL receive commands without directory path parameters.

#### Scenario: Worker lifecycle
- **WHEN** a worker completes its task
- **THEN** it SHALL tell the parent with a result message (carrying only nzoId, no paths) and stop itself

#### Scenario: Worker failure
- **WHEN** a worker throws an unhandled exception
- **THEN** it SHALL be stopped (Directive.Stop supervision) and the coordinator SHALL persist JobFailed

### Requirement: FailureKind classification
Workers SHALL classify failures as `Gone` (404/410), `Transient` (5xx/timeout), `Malformed` (FFmpeg error), or `LocalIo` (disk full/permission). The coordinator SHALL use this classification to decide the failure response.

#### Scenario: Gone failure
- **WHEN** a download returns HTTP 404
- **THEN** the coordinator SHALL immediately fail the job with FailureKind.Gone

### Requirement: Category threading to workers
The DownloadActor SHALL pass category to workers that need output path resolution. Specifically, `RemuxVideo` SHALL include the category so `FfmpegService` can call `FileService.GetOutputPath(title, category)` and `FileService.EnsureOutputDirectory(title, category)`.

#### Scenario: Category passed to RemuxActor
- **WHEN** the coordinator enters the Muxing stage with category `"tv"`
- **THEN** `RemuxVideo` SHALL include `category: "tv"` alongside nzoId, title, and hasSubtitle

### Requirement: Status updates to DownloadRequestActor
On each stage transition, the DownloadActor SHALL tell the DownloadRequestActor shard with `UpdateStatus`. On completion, it SHALL tell `MarkCompleted`. On failure, it SHALL tell `MarkFailed`.

#### Scenario: Status forwarded on stage change
- **WHEN** the coordinator enters the Muxing stage
- **THEN** it SHALL tell DownloadRequestActor with `UpdateStatus(nzoId, "Muxing")`

### Requirement: Completion notification to QueueActor
On job completion or failure, the DownloadActor SHALL tell QueueActor with `NotifyJobFinished(nzoId, outcome)` to free the scheduling slot.

#### Scenario: Slot freed on completion
- **WHEN** the coordinator completes a download
- **THEN** it SHALL tell QueueActor with `NotifyJobFinished(nzoId, "success")`
