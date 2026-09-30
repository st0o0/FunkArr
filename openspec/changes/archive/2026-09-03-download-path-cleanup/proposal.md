## Why

The download file service introduced in `download-categories` left three loose ends: the host project still bootstraps download directories, the DownloadWorker still has inline filesystem calls, and the SABnzbd history response returns a file path where Sonarr/Radarr expect a directory path. Additionally, downloads without episode identifiers (common with Mediathek content — news segments, single docs) collide on the same output path, silently overwriting previous downloads.

## What Changes

- Remove `EnsureDownloadDirectories` from `ApplicationSetupContainer`. The file service self-bootstraps via `IHostedLifecycleService`.
- Move the remaining `Path.GetDirectoryName` + `Directory.CreateDirectory` from `DownloadWorker.HandleStart` into `IDownloadFileService` — the worker calls a single method that ensures the incomplete dir and returns the temp path.
- Fix SABnzbd history `storage` field: derive the directory from the stored file path in the adapter layer. No persistence or message changes.
- Add path collision safety: `ResolveOutputPath` accepts the entity ID and appends a short disambiguator to the output directory name when the title lacks an episode identifier (`S01E01`, `E01`, or `yyyy-MM-dd` pattern).

## Capabilities

### Modified Capabilities
- `download-file-service`: `ResolveTempPath` renamed to `EnsureIncompletePath` (creates dir + returns path). `ResolveOutputPath` gains `entityId` parameter for collision safety. Self-bootstraps directories via hosted service.
- `download-worker`: No more inline `Path.*` or `Directory.*` calls — fully delegates to file service.
- `sabnzbd-download-api`: History `storage` field returns directory path instead of file path.

## Impact

- **FunkArr.Download/DownloadFileService**: Method signature changes, hosted service registration, collision detection logic.
- **FunkArr.Download/DownloadWorker**: Remove last inline filesystem calls, update `ComputePaths` to use new method signatures.
- **FunkArr.Download/DownloadServiceExtensions**: Register `IDownloadFileService` as `IHostedLifecycleService`.
- **FunkArr/Configuration/ApplicationSetupContainer**: Remove `EnsureDownloadDirectories` method entirely.
- **FunkArr/Configuration/ServiceSetupContainer**: Hosted service registration wiring.
- **FunkArr.ArrApi/Sabnzbd/DownloadApiEndpoints**: Derive directory from file path for `Storage`.
- **Tests**: Update file service tests for new signatures, add collision tests.
