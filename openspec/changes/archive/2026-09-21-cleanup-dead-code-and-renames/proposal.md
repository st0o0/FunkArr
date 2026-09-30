## Why

Six snapshot types exist as boilerplate across Download, Scoring, and Search domains but are never called in production code. A dead persistence event (`ScoringRecorded`) was superseded by `HistoryRecorded` when `EnrichedCount` was added. Several Messages types have names that suggest persistence snapshots or events but are actually query responses or commands, creating confusion about the Pathfinder pattern boundaries.

## What Changes

- **Remove 6 dead snapshot types** and their `GetSnapshot()`/`FromSnapshot()` extension methods:
  - `DownloadWorkerSnapshot` — actor returns `WorkerStatusResult` directly
  - `DownloadManagerSnapshot` — actor uses `PaginateQueue` → `QueueResult`
  - `DownloadHistoryManagerSnapshot` — actor returns `HistoryResult` directly
  - `ScoringManagerSnapshot` — `ScoringManager` is not even a persistent actor
  - `MediathekViewWebManagerSnapshot` — pure unused boilerplate
  - `SearchManagerSnapshot` — `FromSnapshot()` returns `Empty` anyway
- **Remove dead persistence event** `ScoringRecorded` from `FunkArr.Persistence/Events/ScoringHistory/` (older version without `EnrichedCount`, fully replaced by `HistoryRecorded`)
- **Rename misleading types**:
  - `StatsUpdated` → `UpdateStats` (it's a command, not an event)
  - `AllStatsSnapshot` → `AllStatsResult` (it's a query response, not a persistence snapshot)
  - `HistoryState.HistorySnapshot` → `HistoryState.HistoryEntry` (it's a history entry record, not an Akka snapshot)
- **Update or remove tests** that only exercise dead code

## Capabilities

### New Capabilities

_(none — this is a cleanup change)_

### Modified Capabilities

_(none — no spec-level behavior changes, purely internal cleanup and renames)_

## Impact

- **FunkArr.Download**: Remove `DownloadWorkerSnapshot`, `DownloadManagerSnapshot`, `DownloadHistoryManagerSnapshot` + their `GetSnapshot()`/`FromSnapshot()` methods from state extension files. Update `DownloadStateSnapshotTests`.
- **FunkArr.Scoring**: Remove `ScoringManagerSnapshot` + methods from `ScoringManagerState.cs`. Update `ScoringManagerStateTests`.
- **FunkArr.Search**: Remove `MediathekViewWebManagerSnapshot`, `SearchManagerSnapshot` + methods from state files.
- **FunkArr.Persistence**: Delete `ScoringRecorded.cs` from `Events/ScoringHistory/`.
- **FunkArr.Messages**: Rename `StatsUpdated` → `UpdateStats`, `AllStatsSnapshot` → `AllStatsResult` in `History/StatsMessages.cs`.
- **FunkArr.History**: Rename `HistoryState.HistorySnapshot` → `HistoryState.HistoryEntry`. Update all references in `HistoryState.cs`, `HistoryWorker.cs`, `HistoryStateTests.cs`.
- **FunkArr.Api**: Update any references to renamed Messages types.
