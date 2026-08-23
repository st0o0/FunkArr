# Capability: Download Coordinator

## Purpose

Orchestrates the lifecycle of a single download job as a ShardRegion entity. Manages an event-sourced stage machine that progresses through fetching, subtitle acquisition, subtitle conversion, and muxing stages via transient child workers, with failure classification and status reporting to the broader download pipeline.

## Requirements

### Requirement: DownloadCoordinator ShardRegion entity
The system SHALL register a `DownloadCoordinator` ShardRegion alongside the existing `DownloadRequestTracker` ShardRegion. Each entity SHALL be identified by nzoId and SHALL be a `ReceivePersistentActor` with `PersistenceId: "download-{nzoId}"`.

#### Scenario: Shard region registered
- **WHEN** the application starts
- **THEN** the DownloadCoordinator ShardRegion SHALL be available and addressable by nzoId

### Requirement: Event-sourced stage machine
The DownloadCoordinator SHALL persist stage transitions: `JobAccepted`, `StageEntered`, `JobCompleted`, `JobFailed`, `JobCancelled`. Recovery SHALL reconstruct the current stage and resume from there.

#### Scenario: Recovery resumes from last stage
- **WHEN** a DownloadCoordinator entity recovers with events showing `StageEntered(Muxing)`
- **THEN** it SHALL resume from the Muxing stage (re-spawn RemuxWorker)

### Requirement: Stage machine flow
The DownloadCoordinator SHALL progress through stages: `Accepted → Fetching → AcquiringSubtitle → ConvertingSubtitle → Muxing → Done`. Each stage spawns a transient child worker.

#### Scenario: Full download pipeline
- **WHEN** `StartDownload` is received with a direct MP4 URL and subtitle URL
- **THEN** the coordinator SHALL progress: spawn DirectDownloadWorker for video → spawn DirectDownloadWorker for subtitle → spawn SubtitleConvertWorker → spawn RemuxWorker → persist JobCompleted

#### Scenario: No subtitle available
- **WHEN** no subtitle URL is provided and the source is not HLS
- **THEN** the coordinator SHALL skip AcquiringSubtitle and ConvertingSubtitle, proceeding directly to Muxing

### Requirement: Five transient child worker types
The DownloadCoordinator SHALL spawn these workers as children: `DirectDownloadWorker` (Mp4DownloadService), `HlsDownloadWorker` (HlsDownloadService), `SubtitleExtractWorker` (SubtitleAcquisitionService), `SubtitleConvertWorker` (SubtitleNormalizer), `RemuxWorker` (MuxingService).

#### Scenario: Worker lifecycle
- **WHEN** a worker completes its task
- **THEN** it SHALL tell the parent with a result message and stop itself

#### Scenario: Worker failure
- **WHEN** a worker throws an unhandled exception
- **THEN** it SHALL be stopped (Directive.Stop supervision) and the coordinator SHALL persist JobFailed

### Requirement: FailureKind classification
Workers SHALL classify failures as `Gone` (404/410), `Transient` (5xx/timeout), `Malformed` (FFmpeg error), or `LocalIo` (disk full/permission). The coordinator SHALL use this classification to decide the failure response.

#### Scenario: Gone failure
- **WHEN** a download returns HTTP 404
- **THEN** the coordinator SHALL immediately fail the job with FailureKind.Gone

### Requirement: Status updates to DownloadRequestTracker
On each stage transition, the DownloadCoordinator SHALL tell the DownloadRequestTracker shard with `UpdateStatus`. On completion, it SHALL tell `MarkCompleted`. On failure, it SHALL tell `MarkFailed`.

#### Scenario: Status forwarded on stage change
- **WHEN** the coordinator enters the Muxing stage
- **THEN** it SHALL tell DownloadRequestTracker with `UpdateStatus(nzoId, "Muxing")`

### Requirement: Completion notification to QueueCoordinator
On job completion or failure, the DownloadCoordinator SHALL tell QueueCoordinator with `NotifyJobFinished(nzoId, outcome)` to free the scheduling slot.

#### Scenario: Slot freed on completion
- **WHEN** the coordinator completes a download
- **THEN** it SHALL tell QueueCoordinator with `NotifyJobFinished(nzoId, "success")`
