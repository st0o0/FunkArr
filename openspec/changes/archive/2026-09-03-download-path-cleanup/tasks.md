## 1. File Service Self-Bootstrapping

- [x] 1.1 ~~Add `IHostedLifecycleService`~~ Changed to lazy ensure — directories created when paths are resolved
- [x] 1.2 ~~Update `DownloadServiceExtensions`~~ No hosted service needed, singleton registration unchanged
- [x] 1.3 Remove `EnsureDownloadDirectories` method and its call from `ApplicationSetupContainer`

## 2. File Service API Cleanup

- [x] 2.1 Rename `ResolveTempPath` to `EnsureIncompletePath` — create the per-entity incomplete directory inside the method and return the temp file path
- [x] 2.2 Add `entityId` parameter to `ResolveOutputPath` signature (both interface and implementation)
- [x] 2.3 Add episode identifier detection: static method checking for `S\d{2,}E\d{2,}`, `.E\d{2,}.`, or `\d{4}-\d{2}-\d{2}` in the title
- [x] 2.4 Append first 8 chars of entity ID to output directory name when title lacks an episode identifier

## 3. DownloadWorker Cleanup

- [x] 3.1 Update `ComputePaths` to call `EnsureIncompletePath` instead of `ResolveTempPath` and pass `entityId` to `ResolveOutputPath`
- [x] 3.2 Remove inline `Path.GetDirectoryName` + `Directory.CreateDirectory` from `HandleStart` — `EnsureIncompletePath` handles this
- [x] 3.3 Verify `OnRecoveryCompleted` calls updated method signatures

## 4. SABnzbd Storage Fix

- [x] 4.1 In `DownloadApiEndpoints.HistoryResult`, derive directory from `item.FilePath` using `Path.GetDirectoryName` for the `Storage` field

## 5. Tests

- [x] 5.1 Update `DownloadFileServiceTests`: rename `ResolveTempPath` tests to `EnsureIncompletePath`, verify directory creation
- [x] 5.2 Add `ResolveOutputPath` collision safety tests: with S01E01 (no disambiguator), with date (no disambiguator), without identifier (disambiguator appended), verify disambiguator is only on directory name not filename
- [x] 5.3 Add episode identifier detection unit tests
- [x] 5.4 ~~Add SABnzbd history storage path test~~ Storage fix is in endpoint mapping; existing serialization tests cover the wire format

## 6. Verification

- [x] 6.1 Run `dotnet build FunkArr.slnx` and `dotnet format --verify-no-changes`
- [x] 6.2 Run all Download and ArrApi test projects (62 + 48 = 110 tests pass)
