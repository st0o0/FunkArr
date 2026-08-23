## MODIFIED Requirements

### Requirement: Progress callback invocation
`DownloadService.DownloadAsync` SHALL accept an `IProgress<DownloadProgress>` parameter and invoke `progress.Report(new DownloadProgress(downloadedBytes, totalBytes))` no more often than every 2 seconds while the video body is being read. `DownloadProgress` SHALL be a class with `long DownloadedBytes` and `long TotalBytes` fields. The callback MUST NOT be invoked for the subtitle download.

#### Scenario: Progress reported during a long download
- **WHEN** a 500MB video download takes longer than 2 seconds to complete
- **THEN** `progress.Report(...)` is invoked one or more times during the download, each call passing a `DownloadProgress` with the cumulative bytes downloaded so far and the total content length from the response's `Content-Length` header (or 0 if absent)

#### Scenario: Progress callback is lightweight and non-blocking
- **WHEN** the caller supplies an `IProgress<DownloadProgress>` backed by a `Progress<T>` with a callback that updates a shared object
- **THEN** `DownloadService` invokes `Report` synchronously inline in the read loop and does not wrap it in a `Task.Run`, `try`/`catch`, or timeout

#### Scenario: Fast download completes without a progress report
- **WHEN** the video download completes in under 2 seconds
- **THEN** `progress.Report(...)` may be invoked zero times before `DownloadAsync` returns

## ADDED Requirements

### Requirement: HLS progress via IProgress
`HlsDownloadService.DownloadAsync` SHALL accept an `IProgress<DownloadProgress>` parameter replacing the `Action<long, long> onProgress` callback. Progress SHALL be reported via `progress.Report(new DownloadProgress(elapsedSeconds, totalDurationSeconds))`.

#### Scenario: HLS progress reported during download
- **WHEN** an HLS download is in progress and FFmpeg reports progress
- **THEN** `progress.Report(...)` is invoked no more often than every 2 seconds with elapsed and total duration

### Requirement: DownloadProgress type
A `DownloadProgress` class SHALL exist in the `FunkArr.DownloadClient` namespace with `long DownloadedBytes` and `long TotalBytes` fields. The fields SHALL use `Volatile.Write` for updates and `Volatile.Read` for reads to ensure visibility across threads without locking.

#### Scenario: Thread-safe reads
- **WHEN** a stream thread writes progress via `Report` and an API thread reads the same object
- **THEN** the API thread sees the latest values without tearing or stale caches

### Requirement: IProgress carried on DownloadRequest
`DownloadRequest` SHALL include an `IProgress<DownloadProgress>` property. Stream stages SHALL use `req.Progress.Report(...)` instead of sending actor messages for progress updates.

#### Scenario: Progress available in download stage
- **WHEN** the download stage processes a `DownloadRequest`
- **THEN** it calls `req.Progress.Report(...)` to report progress without any actor involvement
