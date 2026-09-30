## Why

The current `DownloadQueueActor` combines scheduling (queue order, slot management) with job lifecycle (event persistence, progress tracking, API responses) in a single event-sourced actor. This makes it impossible to crash-isolate scheduling from download work, and Sonarr's frequent `mode=queue` polling hits the same mailbox that handles download events. Extracting a dedicated `QueueCoordinator` creates a single point of control for scheduling while enabling the subsequent separation of download work (Phase 2c) and API reporting (Phase 2b).

## What Changes

- Extract `QueueCoordinator` (Singleton, Event-Sourced) from `DownloadQueueActor`
- QueueCoordinator owns: `_queue` (scheduling order + priority), `_active` (running slots), `_paused` (LocalIo failures), `_maxConcurrent` enforcement
- All lifecycle operations (cancel, pause, resume, prioritize) flow exclusively through QueueCoordinator — controllers never directly address download actors
- Add heartbeat safety-net: periodic timer that asks DownloadRequestTracker for status of active jobs to detect lost `JobFinished` messages
- Persist scheduling events: `JobEnqueued`, `JobStarted`, `JobFinished`, `JobPaused`, `JobResumed`, `JobRemoved`, `PriorityChanged`
- Recovery: replay events to reconstruct queue + active + paused sets; all jobs in `_active` reset to `_queue` and re-started via `TryStartNext()`

## Capabilities

### New Capabilities
- `queue-coordinator`: Singleton event-sourced actor managing download scheduling — queue order, priority, MaxConcurrent enforcement, slot management, heartbeat safety-net

### Modified Capabilities
- `sabnzbd-download-client`: SABnzbd controller routes cancel/pause/resume/prioritize through QueueCoordinator instead of DownloadQueueActor
- `queue-api`: Queue API controller sends lifecycle commands to QueueCoordinator
- `download-pipeline`: DownloadQueueActor delegates scheduling to QueueCoordinator; job offering moves from internal queue to QueueCoordinator-initiated `StartDownload`

## Impact

- **Actors**: New `QueueCoordinator` singleton; `DownloadQueueActor` loses scheduling responsibility but retains job state temporarily (fully replaced in Phase 2b/2c)
- **Persistence**: New `PersistenceId: "download-queue-coordinator"` with its own event types; existing `"download-queue"` journal remains until Phase 2c completes the migration
- **Controllers**: `SabnzbdController` and `QueueController` route lifecycle ops through QueueCoordinator
- **Registration**: `FunkArrActorSystemSetup` adds QueueCoordinator as a Singleton
- **Tests**: New QueueCoordinator actor tests; existing DownloadQueueActor tests adapted for reduced responsibility
