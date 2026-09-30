## Why

File path handling in the download pipeline is scattered across four layers (DownloadOptions, DownloadFileService, DownloadWorker, persistence events) with no single owner. Path computation is mixed with file I/O, the worker holds paths as mutable `string?` fields, and full absolute paths are baked into persistence events — making them break when `DownloadPath` config changes. This needs a clean ownership model before the download pipeline grows further.

### Research context

Microsoft does not provide an official read/write file system abstraction — `IFileProvider` is read-only (static content, view resolution, file watching). The .NET runtime team rejected adding `IFile`/`IDirectory` interfaces (dotnet/runtime#29328). The endorsed pattern for domain services that need write access is **narrow custom interfaces scoped to the domain's needs**, not wrapping the full `System.IO` surface. The community library `System.IO.Abstractions` exists but is overkill when only 3 operations are needed. Microsoft's DDD guidance recommends value objects (sealed records in C#) for composite values without identity — exactly the pattern for bundling related paths.

## What Changes

- Introduce a `DownloadPaths` value object that encapsulates incomplete path, complete path, and a relative path (for persistence). Single static factory method as the only entry point for path computation.
- Move all path/naming logic (category resolution, episode identifier check, directory naming) out of `DownloadOptions` and `DownloadFileService` into the value object's factory method.
- Split `DownloadFileService` into pure path resolution (the value object) and a thin I/O service (`IDownloadFileOperations`) that only does mkdir/move/delete.
- **BREAKING**: Remove `FilePath` from `DownloadSucceeded` and `HistoryRecorded` persistence events. Store only the inputs needed to recompute paths. Derive absolute paths at read time from current config.
- **BREAKING**: Remove `FilePath` from `RecordDownload`, `WorkerStatusResult`, and `HistoryItem` messages — replace with derived paths or relative paths.
- Replace loose `string? _tempOutputPath` / `string? _outputPath` fields in `DownloadWorker` with a single `DownloadPaths?` field.

## Capabilities

### New Capabilities

- `download-path-resolution`: Pure path computation logic — the `DownloadPaths` value object, its factory method, and all naming rules (category dirs, episode identifiers, directory disambiguation).

### Modified Capabilities

- `download-file-service`: Stripped to I/O only — no path computation, just directory creation, file moves, and cleanup. Interface renamed to `IDownloadFileOperations`.
- `download-options`: `ResolveCategoryDir` moves out to path resolution. `CompletePath`/`IncompletePath` remain as base path accessors.
- `download-worker`: Uses `DownloadPaths` value object instead of loose string fields. Passes relative path to persistence.
- `download-history`: `HistoryRecord` and `HistoryRecorded` drop `FilePath`. Path derived at query time from stored title+category+downloadId.
- `download-messages`: `RecordDownload`, `WorkerStatusResult`, `HistoryItem` drop or change `FilePath` parameter.
- `ffmpeg-process`: No requirement change — just receives a path string as before.
- `sabnzbd-download-api`: Adapts to changed `WorkerStatusResult` and `HistoryItem` shapes.

## Impact

- **FunkArr.Core**: `DownloadOptions` loses `ResolveCategoryDir` method.
- **FunkArr.Download**: New `DownloadPaths` record, new `IDownloadFileOperations` interface. `DownloadFileService` and `IDownloadFileService` replaced. `DownloadWorker` refactored. `DownloadHistoryManager` derives paths at query time.
- **FunkArr.Messages**: Three message records changed (breaking parameter changes).
- **FunkArr.Persistence**: Two event records changed (`DownloadSucceeded`, `HistoryRecorded`) — **breaking persistence change** but acceptable at v0.x.
- **FunkArr.ArrApi**: `DownloadApiEndpoints` adapts to new message shapes.
- **Tests**: `DownloadFileServiceTests` rewritten for new structure. New tests for `DownloadPaths` value object.
