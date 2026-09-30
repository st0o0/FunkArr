## Why

HandleQueryQueue fans out Ask calls to ALL workers in the queue, then paginates
the results. For a queue of 50 items requesting page 0-19, 30 Ask results are
thrown away. This wastes actor messages, memory, and time waiting for workers
that won't appear in the result. Additionally, domain enum values are passed as
`int` across message and persistence boundaries, requiring unsafe casts instead
of explicit switch mappings.

## What Changes

- Add `Category` (MediaType) to `QueueEntry` and `Dispatched` in DownloadManagerState
  so category filtering can happen at the ID level without asking workers
- Add `GetPage(QueryQueue)` method to DownloadManagerState that filters + paginates
  on IDs, returning only the IDs needed for the requested page
- Replace Task.WhenAll fan-out with Akka.Streams `SelectAsync` pipeline with
  controlled parallelism and `ResumingDecider` for timeout handling
- Extend `DownloadEnqueued` persistence event with Category field (extend-only)
- Create `PersistedDownloadStatus` enum in Persistence project
- Change `HistoryRecorded.Status` from `int` to `PersistedDownloadStatus`
- Change `WorkerStatusResult.Status` from `int` to `WorkerStatus` enum
- Replace all `DownloadPriority` int-casts with switch mappings
- Add switch mappings for `DownloadStatus` <-> `PersistedDownloadStatus`

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `download-manager`: Pre-pagination on ID-level, Category in state, Akka.Streams query
- `download-worker`: WorkerStatusResult.Status typed as WorkerStatus enum
- `download-history`: HistoryRecorded.Status typed as PersistedDownloadStatus enum
- `akka-persistence`: PersistedDownloadStatus enum, switch mappings for all domain enums

## Impact

- `src/FunkArr.Download/DownloadManager.cs` - HandleQueryQueue rewritten with Akka.Streams
- `src/FunkArr.Download/DownloadManagerState.cs` - QueueEntry + Dispatched extended, GetPage added
- `src/FunkArr.Download/DownloadWorker.cs` - WorkerStatusResult uses WorkerStatus enum
- `src/FunkArr.Download/DownloadHistoryManager.cs` - uses PersistedDownloadStatus
- `src/FunkArr.Download/DownloadHistoryManagerState.cs` - uses PersistedDownloadStatus mapping
- `src/FunkArr.Download/PersistenceMapping.cs` - switch mappings for Priority + Status
- `src/FunkArr.Persistence/Events/Download/DownloadEnqueued.cs` - Category field added
- `src/FunkArr.Persistence/Events/Download/HistoryRecorded.cs` - Status type changed
- `src/FunkArr.Persistence/PersistedDownloadStatus.cs` - new enum
- `src/FunkArr.Messages/Download/WorkerStatusResult.cs` - Status type changed
- Test projects updated for new types
- No API changes (API models stay as-is, mapping in Api layer unchanged)
- **BREAKING** persistence: DownloadEnqueued gets new field (nullable default for recovery)
