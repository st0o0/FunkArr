## 1. Remove dead snapshot types — Download

- [x] 1.1 Remove `DownloadWorkerSnapshot` record and `GetSnapshot()`/`FromSnapshot()` methods from `src/FunkArr.Download/DownloadWorkerState.cs`
- [x] 1.2 Remove `DownloadManagerSnapshot` record and `GetSnapshot()`/`FromSnapshot()` methods from `src/FunkArr.Download/DownloadManagerState.cs`
- [x] 1.3 Remove `DownloadHistoryManagerSnapshot` record and `GetSnapshot()`/`FromSnapshot()` methods from `src/FunkArr.Download/DownloadHistoryManagerState.cs`
- [x] 1.4 Update `src/FunkArr.Download.Tests/DownloadStateSnapshotTests.cs` — remove tests exercising dead snapshot types, keep any remaining valid tests or delete file if empty

## 2. Remove dead snapshot types — Scoring & Search

- [x] 2.1 Remove `ScoringManagerSnapshot` record and `GetSnapshot()`/`FromSnapshot()` methods from `src/FunkArr.Scoring/ScoringManagerState.cs`
- [x] 2.2 Update `src/FunkArr.Scoring.Tests/` — remove snapshot roundtrip tests for `ScoringManagerSnapshot`
- [x] 2.3 Remove `MediathekViewWebManagerSnapshot` record and `GetSnapshot()`/`FromSnapshot()` methods from `src/FunkArr.Search/MediathekViewWebManagerState.cs`
- [x] 2.4 Remove `SearchManagerSnapshot` record and `GetSnapshot()`/`FromSnapshot()` methods from `src/FunkArr.Search/SearchManagerState.cs`

## 3. Remove dead persistence event

- [x] 3.1 Delete `src/FunkArr.Persistence/Events/ScoringHistory/ScoringRecorded.cs`
- [x] 3.2 Remove any remaining references to `ScoringRecorded` (grep and fix compile errors)

## 4. Rename misleading types

- [x] 4.1 Rename `StatsUpdated` → `UpdateStats` in `src/FunkArr.Messages/History/StatsMessages.cs` and all references (HistoryWorker, StatsCollector, StatsCollectorState)
- [x] 4.2 Rename `AllStatsSnapshot` → `AllStatsResult` in `src/FunkArr.Messages/History/StatsMessages.cs` and all references (StatsCollector, StatsCollectorState, RuleSetApiEndpoints)
- [x] 4.3 Rename `HistoryState.HistorySnapshot` → `HistoryState.HistoryEntry` in `src/FunkArr.History/HistoryState.cs` and all references (HistoryWorker, HistoryStateTests, Persistence mapping)

## 5. Verify

- [x] 5.1 Run `dotnet build src/FunkArr.slnx` — zero errors
- [x] 5.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes` — passes
- [x] 5.3 Run all test projects — all pass
