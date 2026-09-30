## Why

The current download pipeline uses a single Akka.Streams graph (`Source.Queue → Download → Subtitle → Mux → Sink`) materialized inside `DownloadPipelineActor`. This has three problems: (1) cancelling a single download requires killing the entire stream and re-materializing it, losing all in-flight jobs; (2) failure in one stage (e.g., FFmpeg crash during mux) causes a stream restart that resets all downloads; (3) progress, retry, and pause/resume semantics are nearly impossible to implement per-job in a shared stream graph. Replacing the stream with a per-download `DownloadCoordinator` shard entity that spawns stage workers as children gives per-job isolation, persistence, cancellation, and failure classification.

## What Changes

- Replace `DownloadPipelineActor` (Akka.Streams graph) with `DownloadCoordinator` ShardRegion (one entity per nzoId)
- DownloadCoordinator is event-sourced (Tier 1): persists `JobAccepted`, `StageEntered`, `JobFailed`, `JobCompleted`, `JobCancelled`, `RetryAttempted`
- Stage machine orchestration: `Queued → Fetching → AcquiringSubtitle → ConvertingSubtitle → Muxing → Done`
- Five stage workers as children (spawned per stage, transient):
  - `DirectDownloadWorker` — Akka.Streams for MP4/subtitle byte transport with KillSwitch, bandwidth throttle, resume via Range header
  - `HlsDownloadWorker` — FFmpeg process for HLS video download
  - `SubtitleExtractWorker` — FFprobe + FFmpeg for HLS embedded subtitle extraction
  - `SubtitleConvertWorker` — FFmpeg for VTT/TTML → SRT conversion
  - `RemuxWorker` — FFmpeg for final MKV assembly
- Failure taxonomy: `Gone | Transient | Malformed | LocalIo` with per-kind retry/fail/pause behavior
- Supervision: `Directive.Stop` for all children — no blind restart; child classifies failure before stopping
- Cancel: QueueCoordinator tells DownloadCoordinator, which kills current child worker
- Pause on `LocalIo` failure: releases QueueCoordinator slot, resume re-enqueues at front
- Remove `DownloadQueueActor` and `DownloadPipelineActor` entirely
- Remove `DownloadQueueState` (replaced by QueueCoordinator + DownloadCoordinator persistence)

## Capabilities

### New Capabilities
- `download-coordinator`: ShardRegion entity (per nzoId) with event-sourced stage machine — spawns transient child workers per download stage, reports status to DownloadRequestTracker and completion to QueueCoordinator
- `direct-download-worker`: Akka.Streams-based byte transport actor for MP4 video and subtitle files with KillSwitch cancel, bandwidth throttle, Conflate+Throttle progress, and FileIO.Append resume
- `hls-download-worker`: FFmpeg process actor for HLS video download with progress parsing and failure classification
- `subtitle-workers`: SubtitleExtractWorker (ffprobe + ffmpeg for HLS subtitles) and SubtitleConvertWorker (VTT/TTML/XML → SRT)
- `remux-worker`: FFmpeg process actor for final video + subtitle → MKV assembly with atomic File.Move before completion status
- `download-failure-taxonomy`: FailureKind enum (Gone/Transient/Malformed/LocalIo) with per-kind retry, permanent failure, or pause behavior and persisted RetryAttempted events

### Modified Capabilities
- `persistence-dtos`: New DTO types for DownloadCoordinator events; migration path from old DownloadQueueActor events
- `download-service`: Mp4DownloadService, HlsDownloadService move from stream stages into worker actors
- `muxing-pipeline`: MuxingService moves from stream stage into RemuxWorker actor
- `subtitle-processing`: SubtitleAcquisitionService logic moves into SubtitleExtractWorker and SubtitleConvertWorker
- `stream-supervision`: Stream supervision removed (no more shared stream graph); replaced by per-worker supervision strategy

## Impact

- **Actors**: `DownloadQueueActor` and `DownloadPipelineActor` deleted; replaced by `DownloadCoordinator` shard + 5 worker types
- **Akka.Streams**: Stream graph removed from download path; Akka.Streams retained only inside `DirectDownloadWorker` for byte transport
- **Persistence**: New `PersistenceId: "download-{nzoId}"` per entity; old `"download-queue"` journal becomes obsolete (migration: replay old events into new QueueCoordinator + DownloadCoordinator, or clean cut with fresh journal)
- **Services**: `Mp4DownloadService`, `HlsDownloadService`, `MuxingService`, `SubtitleAcquisitionService` remain as service classes but are consumed by workers instead of stream stages
- **Stream stages**: `DownloadStages.cs`, `SubtitleStages.cs`, `MuxStages.cs` deleted
- **Tests**: Complete rewrite of download tests — per-worker TestKit tests + coordinator stage machine tests
- **Dependencies**: Akka.Cluster.Sharding for DownloadCoordinator (single-node, shared setup with DownloadRequestTracker)
