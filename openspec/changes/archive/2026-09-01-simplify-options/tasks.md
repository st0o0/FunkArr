## 1. Move and refactor FunkArrOptions

- [x] 1.1 Move `FunkArrOptions` from `FunkArr/Configuration/` to `FunkArr.Core/`, replace `PersistencePath` and `DownloadPath` with `DataPath` property and computed read-only `PersistencePath`, `DownloadPath`, `RuleSetDataPath`
- [x] 1.2 Remove `DataPath` from `RuleSetUpdaterOptions`

## 2. Update consumers

- [x] 2.1 Update `AkkaSetupContainer` to use `FunkArrOptions.PersistencePath` (already does, just verify after move)
- [x] 2.2 Update `RuleSetUpdater` to inject `IOptionsMonitor<FunkArrOptions>` for `RuleSetDataPath` instead of `RuleSetUpdaterOptions.DataPath`
- [x] 2.3 Update `DownloadApiEndpoints` to use `IOptionsMonitor<FunkArrOptions>.DownloadPath` instead of reading `IConfiguration` directly

## 3. Configuration files and DI

- [x] 3.1 Update `appsettings.json` — replace `PersistencePath` with `DataPath`, remove `RuleSet.DataPath`
- [x] 3.2 Update `ServiceSetupContainer` — namespace already resolves via `using FunkArr.Core`
- [x] 3.3 Update `TestOptionsMonitor` usages if affected — no tests reference these options

## 4. Verify

- [x] 4.1 Build solution, run `dotnet format`, run all test projects
