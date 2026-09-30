## Why

The download queue is strictly FIFO with no way to reorder items. Users cannot prioritize urgent downloads or deprioritize bulk imports. Sonarr/Radarr send priority values via the SABnzbd API that are silently ignored. Adding queue reordering and priority support makes the download pipeline controllable and SABnzbd-compatible.

## What Changes

- **BREAKING**: `DownloadManagerState.Queued` changes from `IReadOnlyList<Guid>` to `IReadOnlyList<QueueEntry>` with per-item priority
- **BREAKING**: `DownloadManagerState.Dispatched` changes from `IReadOnlySet<Guid>` to `IReadOnlyDictionary<Guid, DownloadPriority>` to preserve priority through dispatch and recovery
- **BREAKING**: `DownloadEnqueued` persistence event gains a `DownloadPriority` field (no migration — 0.2.0 break)
- New `DownloadPriority` enum (`Low = -1`, `Normal = 0`, `High = 1`)
- New `MoveDownload`, `SwapDownloads`, `SetDownloadPriority` commands with corresponding persistence events
- `AddDownload` gains optional `DownloadPriority` parameter (default `Normal`)
- `QueueItem` response gains `Priority` field
- Internal API: three new endpoints for move, swap, and priority
- SABnzbd API: `name=priority` and `name=switch` queue operations, `addfile` reads priority parameter, `QueueSlot.Priority` returns actual value instead of hardcoded "Normal"
- SABnzbd Force priority (2) maps to existing `ForceStartDownload` rather than a queue priority

## Capabilities

### New Capabilities

- `download-queue-reorder`: Queue manipulation operations — move-to-position, swap, and priority buckets for the download pipeline
- `download-priority`: Priority enum, priority-aware dispatch ordering, and SABnzbd priority mapping

### Modified Capabilities

- `download-messages`: New command/response types for move, swap, and priority; `AddDownload` extended with priority; `QueueItem` extended with priority field
- `download-queue-ui`: Queue display needs priority indicators and reorder controls (drag-and-drop or buttons)
- `arr-api-structure`: SABnzbd API gains `value2` parameter and new queue operation modes (`priority`, `switch`)

## Impact

- **FunkArr.Messages**: New message types, extended `AddDownload` and `QueueItem`
- **FunkArr.Persistence**: New events (`DownloadMoved`, `DownloadSwapped`, `DownloadPriorityChanged`), extended `DownloadEnqueued`; new `DownloadPriority` enum
- **FunkArr.Download**: `DownloadManagerState` restructured, new Apply methods, `DownloadManager` handles new commands
- **FunkArr.Api**: Three new endpoints under `/api/downloads/queue/`
- **FunkArr.ArrApi**: SABnzbd queue handler extended, `DownloadGetRequest` gains `value2`, `addfile` reads priority
- **FunkArr.Download.Tests**: Tests for state manipulation (move, swap, priority change, bucket clamping, same-bucket swap constraint)
- **FunkArr.UI**: Queue view needs priority display and reorder UI (separate change)
