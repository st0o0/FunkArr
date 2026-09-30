## Why

The download pipeline has blurred actor responsibilities and several correctness issues. History tracking is scattered across QueueActor (maintains `CompletedJobIds`) and DownloadRequestActor (answers `QueryHistory` via expensive N-way fan-out). QueueActor mixes scheduling with history bookkeeping. DownloadRequestActor serves both live-status and post-completion queries despite these having different lifecycles. Additionally: `HasSubtitle` is not event-sourced (crash-recovery inconsistency), unhandled worker `Terminated` messages leave downloads stuck indefinitely, dead code artifacts create confusion, no snapshot support degrades restart performance, and the SABnzbd API returns no byte-level download progress.

## What Changes

- **Introduce HistoryActor** — new singleton persistent actor that owns all post-completion concerns: history queries, retry info, history management (remove/clear). Receives completion/failure notifications from DownloadActor. Supports snapshots.
- **Slim DownloadRequestActor** — remove `QueryHistory` and `QueryRetryInfo` (moved to HistoryActor). Becomes a pure live-status tracker for active downloads. Adds in-memory byte-level progress fields. Can passivate after terminal state.
- **Slim QueueActor** — remove `CompletedJobIds`, `GetCompletedJobIds`, `RemoveFromHistory`, `JobRemovedFromHistory`. Only manages pending queue + active set. Add snapshot support.
- **Fix HasSubtitle persistence** — add `SubtitleDetected` event to DownloadActor so recovery correctly restores the flag.
- **Fix Terminated handling** — DownloadActor transitions to Failed when receiving `Terminated` for a worker without a prior `WorkerFailed`. Prevents stuck downloads.
- **Remove dead code** — delete unused `DownloadJob.cs`, `DownloadOutcome.cs`, `DownloadProgress.cs`. Remove vestigial `TempPath`/`OutputDir` fields from `DcJobAccepted` DTO (keep backward-compatible deserialization).
- **Progress reporting** — worker actors send periodic `ProgressTick` to DownloadActor, forwarded to DownloadRequestActor as in-memory state. `QueryStatus` response includes `percentage`/`mb`/`mbleft` for the SABnzbd queue API.
- **Snapshot support** — QueueActor and HistoryActor save snapshots periodically. Recovery loads latest snapshot + replays only subsequent events.
- **Tests** — add QueueActor and HistoryActor test coverage.
- **Rename** — `HlsDownloadServiceTests.cs` → `DownloadSourceDetectorTests.cs` (matches actual content).

## Capabilities

### New Capabilities

- `download-history`: Dedicated HistoryActor for post-completion record keeping, history queries, retry info, and history management with snapshot support.
- `download-progress`: Byte-level progress reporting from worker actors through DownloadActor to DownloadRequestActor, exposed via SABnzbd queue API fields.

### Modified Capabilities

- `download-coordinator`: Add `SubtitleDetected` event for correct recovery. Fix `Terminated` handling to transition to Failed. Forward progress ticks. Notify HistoryActor on completion/failure.
- `queue-coordinator`: Remove history tracking responsibility (`CompletedJobIds`, `RemoveFromHistory`). Add snapshot support.
- `download-request-tracker`: Remove `QueryHistory` and `QueryRetryInfo`. Add in-memory progress fields to `QueryStatus` response. Support passivation after terminal state.
- `sabnzbd-download-client`: Queue endpoint returns real progress values (`percentage`, `mb`, `mbleft`). History endpoint queries HistoryActor instead of fan-out. Retry endpoint queries HistoryActor for retry info.
- `persistence-dtos`: Add `SubtitleDetected` DTO to download coordinator journal. Add HistoryActor journal DTOs. Remove vestigial fields from `DcJobAccepted` (backward-compatible).

## Impact

- **Code**: `DownloadClient/Pipeline/`, `DownloadClient/Tracker/`, `DownloadClient/Queue/`, `Persistence/`, SABnzbd API controller
- **New files**: HistoryActor + state + events + journal DTOs
- **Deleted files**: `DownloadJob.cs`, `DownloadOutcome.cs`, `DownloadProgress.cs`
- **Renamed files**: `HlsDownloadServiceTests.cs` → `DownloadSourceDetectorTests.cs`
- **APIs**: SABnzbd queue response gains real progress fields. SABnzbd history/retry endpoints change data source (transparent to consumers).
- **Persistence**: New journal for HistoryActor. New event type in DownloadActor journal. New snapshot types for QueueActor and HistoryActor. **BREAKING** for QueueActor journal (removed `JobRemovedFromHistory` event) — acceptable at v0.x.
