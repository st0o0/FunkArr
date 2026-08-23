## ADDED Requirements

### Requirement: Chunked HTTP video download

`DownloadService` SHALL download the video content of a `DownloadRequest` via
HTTP GET using `HttpCompletionOption.ResponseHeadersRead`, then stream the
response body to a temp file in 8192-byte chunks rather than buffering the
whole response in memory. This method SHALL only handle direct HTTP downloads
(.mp4 URLs). HLS downloads are handled by a separate flow.

#### Scenario: Successful video download

- **WHEN** `DownloadService.DownloadAsync` is called with a `DownloadRequest`
  whose `VideoUrl` returns a 200 response with a video body
- **THEN** the response is read in 8192-byte chunks and each chunk is written
  to the temp file as it is read, and the method returns the temp file path
  as `VideoPath`

#### Scenario: Video download fails with non-success status

- **WHEN** the HTTP GET for `VideoUrl` returns a non-success status code
- **THEN** `DownloadService.DownloadAsync` throws (via
  `HttpResponseMessage.EnsureSuccessStatusCode`), and no partial temp file is
  left open for writing

### Requirement: Progress callback invocation

`DownloadService.DownloadAsync` SHALL accept an `IProgress<DownloadProgress>` parameter and invoke `progress.Report(new DownloadProgress(downloadedBytes, totalBytes))` no more often than every 2 seconds while the video body is being read. `DownloadProgress` SHALL be a class with `long DownloadedBytes` and `long TotalBytes` fields. The callback MUST NOT be invoked for the subtitle download.

#### Scenario: Progress reported during a long download

- **WHEN** a 500MB video download takes longer than 2 seconds to complete
- **THEN** `progress.Report(...)` is invoked one or more times during the
  download, each call passing a `DownloadProgress` with the cumulative bytes
  downloaded so far and the total content length from the response's
  `Content-Length` header (or 0 if absent)

#### Scenario: Progress callback is lightweight and non-blocking

- **WHEN** the caller supplies an `IProgress<DownloadProgress>` backed by a
  `Progress<T>` with a callback that updates a shared object
- **THEN** `DownloadService` invokes `Report` synchronously inline in the
  read loop and does not wrap it in a `Task.Run`, `try`/`catch`, or timeout

#### Scenario: Fast download completes without a progress report

- **WHEN** the video download completes in under 2 seconds
- **THEN** `progress.Report(...)` may be invoked zero times before
  `DownloadAsync` returns

### Requirement: Cancellation support

`DownloadService.DownloadAsync` SHALL accept a `CancellationToken` and
propagate it to the underlying HTTP requests and stream reads/writes.

#### Scenario: Cancellation stops an in-progress download

- **WHEN** the supplied `CancellationToken` is cancelled while the video
  stream is being read
- **THEN** the read/write loop observes the cancellation (via
  `OperationCanceledException` from the underlying stream or HTTP call) and
  `DownloadAsync`'s returned `Task` transitions to canceled/faulted rather
  than completing successfully

#### Scenario: Default token allows unrestricted download

- **WHEN** `DownloadAsync` is called without an explicit `CancellationToken`
  (or with `CancellationToken.None`)
- **THEN** the download proceeds to completion or failure exactly as it did
  before cancellation support was added, with no behavior change

### Requirement: Framework-independent service shape

`DownloadService` SHALL be a plain class with no dependency on Akka.NET
actor types (`IActorRef`, `ActorSystem`, etc.), so it can be constructed and
tested with `IHttpClientFactory` and `IFileService` alone, without an
`ActorSystem` or `TestKit`.

#### Scenario: Unit test without an actor system

- **WHEN** a test constructs `DownloadService` with a mocked
  `IHttpClientFactory` (backed by a fake `HttpMessageHandler`) and a fake
  `IFileService`
- **THEN** `DownloadAsync` can be invoked and asserted against directly, with
  no `TestKit`, `ActorSystem`, or stream materialization involved

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
