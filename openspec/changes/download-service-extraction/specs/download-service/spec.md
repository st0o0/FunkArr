## ADDED Requirements

### Requirement: Chunked HTTP video download

`DownloadService` SHALL download the video content of a `DownloadRequest` via
HTTP GET using `HttpCompletionOption.ResponseHeadersRead`, then stream the
response body to a temp file in 8192-byte chunks rather than buffering the
whole response in memory.

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

### Requirement: Subtitle download with graceful fallback

`DownloadService` SHALL attempt to download a subtitle file when
`DownloadRequest.SubtitleUrl` is not null. A non-success response MUST NOT
throw or fail the overall download — it MUST be treated as "no subtitle
available."

#### Scenario: Subtitle downloads successfully

- **WHEN** `DownloadRequest.SubtitleUrl` is set and the HTTP GET for it
  returns a success status code
- **THEN** the subtitle content is written via `IFileService.WriteSubtitleAsync`
  to the path returned by `IFileService.GetTempSubtitlePath`, and
  `DownloadAsync` returns that path as `SubtitlePath`

#### Scenario: Subtitle fetch fails

- **WHEN** `DownloadRequest.SubtitleUrl` is set and the HTTP GET for it
  returns a non-success status code (e.g. 404)
- **THEN** `DownloadAsync` does not throw, logs a warning identifying the
  `NzoId` and the response status, and returns `SubtitlePath` as `null`

#### Scenario: No subtitle requested

- **WHEN** `DownloadRequest.SubtitleUrl` is `null`
- **THEN** `DownloadAsync` does not attempt any subtitle HTTP request and
  returns `SubtitlePath` as `null`

### Requirement: Temp file management via IFileService

`DownloadService` SHALL resolve all temp file paths through `IFileService`
rather than constructing paths itself, so path conventions stay centralized.

#### Scenario: Video temp path resolution

- **WHEN** `DownloadAsync` begins writing the video stream
- **THEN** the destination file path is obtained from
  `IFileService.GetTempVideoPath(request.TempPath, request.NzoId)` and no
  other path construction logic is used

#### Scenario: Subtitle temp path resolution

- **WHEN** a subtitle download succeeds
- **THEN** the destination file path is obtained from
  `IFileService.GetTempSubtitlePath(request.TempPath, request.NzoId)`

### Requirement: Progress callback invocation

`DownloadService.DownloadAsync` SHALL accept an `Action<long, long>` progress
callback and invoke it with `(downloadedBytes, totalBytes)` no more often
than every 2 seconds while the video body is being read, using the same
timing logic as the pre-extraction implementation (`DateTimeOffset.UtcNow`
comparison against the last report time). The callback MUST NOT be invoked
for the subtitle download.

#### Scenario: Progress reported during a long download

- **WHEN** a 500MB video download takes longer than 2 seconds to complete
- **THEN** the `onProgress` callback is invoked one or more times during the
  download, each call passing the cumulative bytes downloaded so far and the
  total content length from the response's `Content-Length` header (or 0 if
  absent)

#### Scenario: Progress callback is lightweight and non-blocking

- **WHEN** the actor supplies an `onProgress` callback that performs a
  fire-and-forget `self.Tell(...)`
- **THEN** `DownloadService` invokes the callback synchronously inline in the
  read loop and does not wrap it in a `Task.Run`, `try`/`catch`, or timeout —
  callers are responsible for keeping the callback fast and
  non-throwing

#### Scenario: Fast download completes without a progress report

- **WHEN** the video download completes in under 2 seconds
- **THEN** `onProgress` may be invoked zero times before `DownloadAsync`
  returns; this is acceptable because progress reporting is best-effort, not
  guaranteed-at-least-once

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
