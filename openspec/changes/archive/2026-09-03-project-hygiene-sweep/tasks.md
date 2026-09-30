## 1. FunkArrOptions + Dead Code Cleanup

- [x] 1.1 Add `LocalRuleSetDataPath => Path.Combine(DataPath, "local")` to `FunkArrOptions`
- [x] 1.2 Remove `IDownloadResponse` interface and all `: IDownloadResponse` from 6 message types
- [x] 1.3 Remove `using Akka.IO` from `MediathekViewWebManager.cs`

## 2. RuleSetManager Overhaul

- [x] 2.1 Add `IOptionsMonitor<FunkArrOptions>` DI constructor to `RuleSetManager`
- [x] 2.2 Add `PreStart()` that reads Options, resolves paths with `Path.GetFullPath`, runs initial scan, and sets up watchers
- [x] 2.3 Make `ScanRuleSets` parameterless (remove `string DataDirectory`), remove `_dataDirectory` field
- [x] 2.4 Switch `RuleSetManagerState` to `ImmutableDictionary<string, RuleSetPaths>` and `ImmutableHashSet<string>` — update all mutation sites to immutable operations
- [x] 2.5 Update `RuleSetManagerTests` — use parameterless `ScanRuleSets`, inject `IOptionsMonitor<FunkArrOptions>` via DI or test helper

## 3. DownloadHistoryActor → DownloadHistoryManager Rename

- [x] 3.1 Rename `IDownloadHistory` → `IDownloadHistoryManager` in `ActorKeys.cs`
- [x] 3.2 Rename `DownloadHistoryActor` → `DownloadHistoryManager` (class + file), rename state class + file accordingly
- [x] 3.3 Update `AkkaSetupContainer` registration: use `resolver.Props<DownloadHistoryManager>()` with key `IDownloadHistoryManager`
- [x] 3.4 Update all references: `DownloadWorker`, `DownloadApiEndpoints`, `QueueApiEndpoints`, internal API endpoints

## 4. QueryQueue Pagination

- [x] 4.1 Add `TotalItems` (int) field to `QueueResult` record
- [x] 4.2 Implement Category filter, Start/Limit pagination in `DownloadManagerState` (extension method on collected worker responses)
- [x] 4.3 Update `HandleQueryQueue` in `DownloadManager` to pass `QueryQueue` params to state method
- [x] 4.4 Add `DownloadManagerStateTests` for pagination: category filter, start offset, limit, limit=0 means all

## 5. QueryHistory Pagination

- [x] 5.1 Add `TotalItems` (int) field to `HistoryResult` record
- [x] 5.2 Implement Category filter, Start/Limit pagination in `DownloadHistoryManagerState` (extension method on history list)
- [x] 5.3 Update `HandleQueryHistory` in `DownloadHistoryManager` to pass `QueryHistory` params to state method
- [x] 5.4 Add tests for history pagination: category filter, start offset, limit, limit=0 means all

## 6. Path Resolution + Final Cleanup

- [x] 6.1 Add `Path.GetFullPath` wrapping in `DownloadWorker.ComputePaths` for incomplete and output directories
- [x] 6.2 Run `dotnet format` across solution
- [x] 6.3 Run all tests, verify build
- [x] 6.4 Update main specs via `/opsx:sync`
