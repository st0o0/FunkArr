## 1. Remove dead DownloadPhaseChanged code

- [x] 1.1 Delete `FunkArr.Persistence/Events/Download/DownloadPhaseChanged.cs`
- [x] 1.2 Remove `Recover<DownloadPhaseChanged>` handler from `DownloadWorker.cs`
- [x] 1.3 Remove `Apply(DownloadPhaseChanged)` method from `DownloadWorkerState.cs`
- [x] 1.4 Remove `DownloadPhase.ToPersistence()` and `ToDomain()` mapping methods from `PersistenceMapping.cs`
- [x] 1.5 Remove test covering `DownloadPhaseChanged` from `DownloadWorkerStateTests.cs`
- [x] 1.6 Remove `DownloadPhase` enum from persistence if unused after cleanup

## 2. Rename Download HistoryRecorded

- [x] 2.1 Rename record `HistoryRecorded` to `DownloadHistoryRecorded` in `FunkArr.Persistence/Events/Download/HistoryRecorded.cs`, rename file to `DownloadHistoryRecorded.cs`
- [x] 2.2 Update all references in `FunkArr.Download/` (DownloadHistoryManager, PersistenceMapping)
- [x] 2.3 Update all references in `FunkArr.Download.Tests/`

## 3. Rename ScoringHistory HistoryRecorded

- [x] 3.1 Rename record `HistoryRecorded` to `ScoringHistoryRecorded` in `FunkArr.Persistence/Events/ScoringHistory/HistoryRecorded.cs`, rename file to `ScoringHistoryRecorded.cs`
- [x] 3.2 Update all references in `FunkArr.History/` (HistoryWorker, HistoryState, PersistenceMapping)
- [x] 3.3 Update all references in `FunkArr.History.Tests/`

## 4. Verify

- [x] 4.1 `dotnet build src/FunkArr.slnx`
- [x] 4.2 Run all test projects
- [x] 4.3 `dotnet format src/FunkArr.slnx --verify-no-changes`
