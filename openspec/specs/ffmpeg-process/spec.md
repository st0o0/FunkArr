# FFmpeg Process

## Purpose

FFmpeg process spawning via FFMpegCore, argument building (direct/HLS, with/without subtitle), progress reporting, explicit stream mapping, per-stream codec copy, and DI-injectable runner interface.

## Requirements

### Requirement: FFmpeg always maps video and audio streams explicitly
The argument builder SHALL always apply `-map 0:v:0 -map 0:a:0` using FFMpegCore's `SelectStream` API to select only the first video and first audio stream.

#### Scenario: Direct MP4 with data track
- **WHEN** building arguments for a direct MP4 URL
- **THEN** FFmpeg SHALL be invoked with `-map 0:v:0 -map 0:a:0`

#### Scenario: HLS with timed_id3 track
- **WHEN** building arguments for an HLS URL containing a `timed_id3` data stream
- **THEN** FFmpeg SHALL be invoked with `-map 0:v:0 -map 0:a:0`
- **AND** the `timed_id3` stream SHALL not appear in the output

### Requirement: FFmpeg applies BSF tolerance for HLS audio
When `RemuxOptions.IsHls` is true, the argument builder SHALL apply `-bsf:a aac_adtstoasc=no_validation=1` to tolerate corrupt AAC ADTS frame headers.

#### Scenario: HLS input gets BSF tolerance
- **WHEN** building arguments with `IsHls` true
- **THEN** FFmpeg SHALL be invoked with `-bsf:a aac_adtstoasc=no_validation=1`

#### Scenario: Direct MP4 does not get BSF tolerance
- **WHEN** building arguments with `IsHls` false
- **THEN** FFmpeg SHALL NOT include any `-bsf:a` argument

### Requirement: FFmpeg uses fluent API for codec selection
The argument builder SHALL use FFMpegCore's `CopyChannel(Channel.Video)` and `CopyChannel(Channel.Audio)` for per-stream codec selection instead of `WithCopyCodec()`.

#### Scenario: Per-stream copy
- **WHEN** building arguments for any input
- **THEN** the builder SHALL use `CopyChannel(Channel.Video)` and `CopyChannel(Channel.Audio)`
- **AND** SHALL NOT use `WithCopyCodec()`

### Requirement: FFmpeg argument builder for direct HTTP video
The system SHALL use FFMpegCore's fluent API to build FFmpeg arguments for downloading direct HTTP video sources (.mp4) with explicit stream mapping and per-stream codec copy into MKV format.

#### Scenario: Direct HTTP without subtitle
- **WHEN** a download is started for a direct MP4 URL and no subtitle
- **THEN** FFMpegCore SHALL be invoked with `FromUrlInput`, `SelectStream` for video and audio, `CopyChannel` for both streams, and `-progress pipe:1`

#### Scenario: Direct HTTP with subtitle
- **WHEN** a download is started for a direct MP4 URL and a subtitle path
- **THEN** FFMpegCore SHALL be invoked with `FromUrlInput` for the video, `AddFileInput` for the subtitle, `SelectStream` for video and audio, `CopyChannel` for both streams, `-c:s srt`, `-disposition:s:0 0`, and `-metadata:s:s:0 language=<lang>`

### Requirement: FFmpeg argument builder for HLS video
The system SHALL use FFMpegCore's fluent API to build FFmpeg arguments for downloading HLS video sources (.m3u8) with explicit stream mapping, per-stream codec copy, and BSF tolerance.

#### Scenario: HLS without subtitle
- **WHEN** a download is started for an HLS URL and no subtitle
- **THEN** FFMpegCore SHALL be invoked with `FromUrlInput`, `SelectStream` for video and audio, `CopyChannel` for both streams, `-bsf:a aac_adtstoasc=no_validation=1`, and `-progress pipe:1`

#### Scenario: HLS with subtitle
- **WHEN** a download is started for an HLS URL and a subtitle path
- **THEN** FFMpegCore SHALL include BSF tolerance in addition to the subtitle arguments

### Requirement: Single builder flow for all input types
The argument builder SHALL use a single code path for building FFmpeg arguments, with conditional additions for subtitle and HLS, instead of duplicated subtitle/no-subtitle branches.

#### Scenario: No code duplication
- **WHEN** building arguments with or without subtitles
- **THEN** the builder SHALL use one `OutputToFile` block with conditional options inside it

### Requirement: FFmpeg progress reporting
The system SHALL report download progress via an `Action<ProgressUpdate>` callback with TotalSize, OutTimeUs, and Speed fields.

#### Scenario: Progress callback during download
- **WHEN** FFmpeg outputs progress data during a download
- **THEN** the runner SHALL invoke the progress callback with parsed TotalSize (bytes), OutTimeUs (microseconds), and Speed (multiplier)

#### Scenario: Speed not available
- **WHEN** FFmpeg outputs `speed=N/A` at the start of a stream
- **THEN** the runner SHALL report Speed=0.0

### Requirement: FFmpeg process management via FFMpegCore
The system SHALL use FFMpegCore's `ProcessAsynchronously` with `throwOnError: true` to manage the FFmpeg process lifecycle, catching `FFMpegException` for error details.

#### Scenario: Process exit success
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the runner SHALL return an `FfmpegResult` with `Success=true`, `ExitCode=0`, `Error=null`, and the elapsed time in seconds

#### Scenario: Process exit failure
- **WHEN** the FFmpeg process exits with a non-zero code
- **THEN** the runner SHALL catch the `FFMpegException`, extract `FFMpegErrorOutput` as stderr, cap it to the last 4096 characters, and return an `FfmpegResult` with `Success=false`, `ExitCode=1`, the capped stderr as `Error`, and the elapsed time in seconds

#### Scenario: Cancellation
- **WHEN** the CancellationToken is cancelled during a running download
- **THEN** the runner SHALL terminate the FFmpeg process and return an `FfmpegResult` with `Success=false`, `ExitCode=-1`, and `Error="Cancelled"`

### Requirement: FFmpeg runner as injectable service
The system SHALL provide `IFfmpegRunner` as a DI-injectable interface with no Akka dependency, registered via an extension method on `IServiceCollection`. The `RunAsync` method SHALL accept `videoUrl`, `subtitlePath`, `outputPath`, `onProgress` callback, and `CancellationToken`. It SHALL NOT accept a speed limit parameter.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** the host SHALL call `AddFfmpegRunner()` extension method, which registers `IFfmpegRunner` as a singleton

#### Scenario: Worker injection
- **WHEN** a DownloadWorker is created
- **THEN** it SHALL receive `IFfmpegRunner` via constructor injection and use it to start FFmpeg processes

#### Scenario: Type visibility
- **WHEN** the FunkArr.Download project is referenced by other projects
- **THEN** `FfmpegRunner` (implementation) SHALL be internal; `IFfmpegRunner`, `FfmpegResult`, and `ProgressUpdate` SHALL be public (required by the public DownloadWorker constructor)

#### Scenario: No speed limit parameter
- **WHEN** `IFfmpegRunner.RunAsync()` is called
- **THEN** the method signature SHALL NOT include a `speedLimitBytesPerSecond` parameter
- **AND** no `-maxrate` or `-bufsize` arguments SHALL be passed to FFmpeg
