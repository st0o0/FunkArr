## Why

The Download domain contains dead code from the DownloadWorker rewrite (`DownloadPhaseChanged` persistence event, recovery handler, Apply method, and persistence mapping methods are never called in production). Additionally, two persistence events named `HistoryRecorded` exist in different namespaces (`Events.Download` and `Events.ScoringHistory`), creating a maintenance hazard when refactoring or using global imports.

## What Changes

- Remove dead `DownloadPhaseChanged` persistence event and all related code (Recover handler, Apply method, mapping extensions, test)
- Rename `FunkArr.Persistence.Events.Download.HistoryRecorded` to `DownloadHistoryRecorded` and update all references
- Rename `FunkArr.Persistence.Events.ScoringHistory.HistoryRecorded` to `ScoringHistoryRecorded` and update all references

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure cleanup with no behavioral or spec-level changes.

## Impact

- `FunkArr.Persistence/Events/Download/` - event record rename + dead code removal
- `FunkArr.Persistence/Events/ScoringHistory/` - event record rename
- `FunkArr.Download/` - DownloadWorker (Recover handler removal), DownloadWorkerState (Apply removal), PersistenceMapping (dead mapper removal), DownloadHistoryManager (rename reference)
- `FunkArr.History/` - HistoryWorker (rename reference), HistoryState (rename reference)
- `FunkArr.Download.Tests/` - remove dead test, update references
- `FunkArr.History.Tests/` - update references
