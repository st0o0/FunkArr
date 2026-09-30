## 1. Config Model

- [x] 1.1 Create `DownloadCategory` class in `FunkArr.Core` with `Name` and `Dir` properties
- [x] 1.2 Create `DownloadOptions` class in `FunkArr.Core` with `DownloadPath`, `ConcurrentDownloads`, `Categories`, `CompletePath`, `IncompletePath`, and `ResolveCategoryDir` method
- [x] 1.3 Remove `DownloadPath` property from `FunkArrOptions` (move to `DownloadOptions`)
- [x] 1.4 Register `DownloadOptions` binding in DI setup (`FunkArr:Download` section)

## 2. Messages & Persistence

- [x] 2.1 Add `IncompletePath` parameter to `InitDownload` record (alongside existing `OutputPath`)
- [x] 2.2 Add `IncompletePath` field to `DownloadInitialized` persistence DTO
- [x] 2.3 Add `IncompletePath` field to `DownloadWorkerState`

## 3. Download Manager

- [x] 3.1 Replace `FunkArrOptions` injection with `DownloadOptions` in `DownloadManager`
- [x] 3.2 Use `DownloadOptions.ConcurrentDownloads` instead of hardcoded `3`
- [x] 3.3 Build `incompletePath` as `Path.Combine(options.IncompletePath, downloadId.ToString())`
- [x] 3.4 Build `outputDir` using `options.ResolveCategoryDir(category)` under `options.CompletePath` with title subfolder
- [x] 3.5 Build `outputPath` as `Path.Combine(outputDir, title + ".mkv")`
- [x] 3.6 Pass both `incompletePath` and `outputPath` to `InitDownload`

## 4. Download Worker

- [x] 4.1 Update `DownloadWorker` to use `IncompletePath` for FFmpeg working directory
- [x] 4.2 Ensure `IncompletePath` directory exists before starting FFmpeg (`Directory.CreateDirectory`)
- [x] 4.3 Ensure output directory (parent of `OutputPath`) exists before completion
- [x] 4.4 Delete `IncompletePath` directory recursively after successful mux (best-effort, log on failure)
- [x] 4.5 Skip cleanup on failure (temp files remain for debugging)

## 5. SABnzbd API

- [x] 5.1 Replace `FunkArrOptions` injection with `DownloadOptions` in `DownloadApiEndpoints`
- [x] 5.2 Update `get_config` to return `complete_dir` as `DownloadOptions.CompletePath`
- [x] 5.3 Update `get_config` categories to dynamically build from `DownloadOptions.Categories`
- [x] 5.4 Update `fullstatus` to return `completedir` as `DownloadOptions.CompletePath`

## 6. Setup Health Check

- [x] 6.1 Replace single directory check with separate checks for `complete/` and `incomplete/` subdirectories
- [x] 6.2 Update `SetupConnectionInfo` to report `CompletePath` as the download path

## 7. Tests

- [x] 7.1 Add unit tests for `DownloadOptions.ResolveCategoryDir` (known, unknown, empty, case-insensitive)
- [x] 7.2 Update `DownloadManagerStateTests` for new path structure
- [x] 7.3 Update `DownloadWorkerStateTests` for `IncompletePath` field
- [x] 7.4 Update `DownloadApiEndpointTests` for new SABnzbd config response
- [x] 7.5 Run `dotnet build FunkArr.slnx` and `dotnet format` to verify
