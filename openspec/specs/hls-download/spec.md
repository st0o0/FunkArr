## Purpose

HLS stream download via FFmpeg: URL type detection, FFmpeg process invocation with stream-copy, and cancellation support for .m3u8 video sources. Implemented as a transient child actor of `DownloadActor`.

## Requirements

### Requirement: HLS stream download via FFmpeg
The `HlsDownloadActor` SHALL be a transient child `ReceiveActor` of `DownloadActor` in namespace `FunkArr.DownloadClient.Pipeline`. It SHALL download HLS video streams (.m3u8 URLs) by delegating to `IFfmpegService.DownloadHlsAsync(nzoId, url)`, which invokes FFmpeg as an external process with `-i "url" -map 0:v -map 0:a -c copy -y output.mp4`. The output SHALL be a regular MP4 file suitable for the remux stage.

#### Scenario: Successful HLS download
- **WHEN** the actor receives a `FetchVideo(nzoId, url)` command
- **THEN** it calls `IFfmpegService.DownloadHlsAsync`, tells the parent `VideoFetched(nzoId)` on success, and stops itself via `Context.Stop(Self)`

#### Scenario: HLS download fails
- **WHEN** `DownloadHlsAsync` throws an exception (e.g. 403, network error, invalid manifest)
- **THEN** the actor classifies the exception via `ClassifyException`, tells the parent `WorkerFailed(nzoId, failureKind, message)`, and stops itself

#### Scenario: HLS download timeout
- **WHEN** an HLS download does not complete within the configured timeout (30 minutes)
- **THEN** the FFmpeg process is killed via `Process.Kill(entireProcessTree: true)` inside `FfmpegService` and the actor reports failure

### Requirement: Exception classification
The `HlsDownloadActor` SHALL classify exceptions into `FailureKind` values to enable the parent `DownloadActor` to decide on retry strategy.

#### Scenario: HTTP 404 or 410 classified as Gone
- **WHEN** the exception is `HttpRequestException` with status `NotFound` or `Gone`
- **THEN** `FailureKind.Gone` is reported (no retry)

#### Scenario: Other HTTP errors classified as Transient
- **WHEN** the exception is a generic `HttpRequestException`
- **THEN** `FailureKind.Transient` is reported (eligible for retry)

#### Scenario: IO errors classified as LocalIo
- **WHEN** the exception is an `IOException`
- **THEN** `FailureKind.LocalIo` is reported

### Requirement: URL type detection
The parent `DownloadActor` SHALL detect whether a video URL is a direct download (.mp4) or an HLS stream (.m3u8) and spawn the appropriate transient child actor (`Mp4DownloadActor` or `HlsDownloadActor`).

#### Scenario: MP4 URL detected
- **WHEN** a download request URL ends with `.mp4` or has Content-Type `video/mp4`
- **THEN** the request is handled by `Mp4DownloadActor`

#### Scenario: M3U8 URL detected
- **WHEN** a download request URL ends with `.m3u8` or has Content-Type `application/x-mpegURL`
- **THEN** the request is handled by `HlsDownloadActor`

### Requirement: Transient child actor lifecycle
The `HlsDownloadActor` SHALL stop itself after completing its work (success or failure) via `Context.Stop(Self)` in a `finally` block.

#### Scenario: Actor self-terminates after work
- **WHEN** the HLS download completes (success or failure)
- **THEN** the actor stops itself, freeing resources in the parent's child collection
