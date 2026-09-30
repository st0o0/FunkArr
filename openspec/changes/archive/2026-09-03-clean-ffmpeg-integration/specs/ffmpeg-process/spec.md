# FFmpeg Process

## MODIFIED Requirements

### Requirement: FFmpeg argument builder for direct HTTP video
The system SHALL use FFMpegCore's fluent API to build FFmpeg arguments for downloading direct HTTP video sources (.mp4) with stream-copy into MKV format.

#### Scenario: Direct HTTP without subtitle
- **WHEN** a download is started for VideoUrl "https://example.com/video.mp4" and no subtitle, with OutputPath "/downloads/output.mkv"
- **THEN** FFMpegCore SHALL be invoked with `FromUrlInput` for the video URL, `OutputToFile` with `overwrite: true`, and `CopyChannel()` for stream-copy

#### Scenario: Direct HTTP with subtitle
- **WHEN** a download is started for VideoUrl "https://example.com/video.mp4" and SubtitleUrl "https://example.com/subs.ttml", with OutputPath "/downloads/output.mkv"
- **THEN** FFMpegCore SHALL be invoked with `FromUrlInput` for the video, `AddUrlInput` for the subtitle, `OutputToFile` with `overwrite: true`, and custom arguments for `-c:v copy -c:a copy -c:s srt -metadata:s:s:0 language=deu`

### Requirement: FFmpeg argument builder for HLS video
The system SHALL use FFMpegCore's fluent API to build FFmpeg arguments for downloading HLS video sources (.m3u8) identically to direct HTTP — FFmpeg handles the protocol transparently.

#### Scenario: HLS without subtitle
- **WHEN** a download is started for VideoUrl "https://example.com/chunklist.m3u8" and no subtitle
- **THEN** FFMpegCore SHALL be invoked with `FromUrlInput` for the HLS URL and `CopyChannel()`, identical to direct HTTP

#### Scenario: HLS with subtitle
- **WHEN** a download is started for an HLS VideoUrl and a SubtitleUrl
- **THEN** FFMpegCore SHALL be invoked with both URL inputs and subtitle custom arguments, identical structure to direct HTTP with subtitle

### Requirement: FFmpeg progress reporting
The system SHALL report download progress via an `Action<ProgressUpdate>` callback with TotalSize, OutTimeUs, and Speed fields.

#### Scenario: Progress callback during download
- **WHEN** FFmpeg outputs progress data during a download
- **THEN** the runner SHALL invoke the progress callback with parsed TotalSize (bytes), OutTimeUs (microseconds), and Speed (multiplier)

#### Scenario: Speed not available
- **WHEN** FFmpeg outputs `speed=N/A` at the start of a stream
- **THEN** the runner SHALL report Speed=0.0

### Requirement: FFmpeg process management via FFMpegCore
The system SHALL use FFMpegCore's `ProcessAsynchronously` to manage the FFmpeg process lifecycle, returning a structured result.

#### Scenario: Process exit success
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the runner SHALL return an `FfmpegResult` with `Success=true`, `ExitCode=0`, `Error=null`, and the elapsed time in seconds

#### Scenario: Process exit failure
- **WHEN** the FFmpeg process exits with a non-zero code
- **THEN** the runner SHALL return an `FfmpegResult` with `Success=false`, the exit code, stderr output as `Error`, and the elapsed time in seconds

#### Scenario: Cancellation
- **WHEN** the CancellationToken is cancelled during a running download
- **THEN** the runner SHALL terminate the FFmpeg process and return an `FfmpegResult` with `Success=false` and `Error="Cancelled"`

### Requirement: FFmpeg runner as injectable service
The system SHALL provide `IFfmpegRunner` as a DI-injectable interface with no Akka dependency.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `IFfmpegRunner` SHALL be registered as a singleton in the service container

#### Scenario: Worker injection
- **WHEN** a DownloadWorker is created
- **THEN** it SHALL receive `IFfmpegRunner` via constructor injection and use it to start FFmpeg processes

## REMOVED Requirements

### Requirement: FFmpeg overwrite handling
**Reason**: FFMpegCore handles overwrite via `OutputToFile(path, overwrite: true)` — no need for manual `-y` flag management.
**Migration**: The `overwrite: true` parameter in FFMpegCore produces the same `-y` flag.
