## MODIFIED Requirements

### Requirement: FFmpeg always maps video and audio streams explicitly
The argument builder SHALL always map the first video and first audio stream from input 0 using FFmpeg.Rx's `StreamBuilder` with `StreamType.Video` and `StreamType.Audio` at input index 0, stream index 0.

#### Scenario: Direct MP4 with data track
- **WHEN** building arguments for a direct MP4 URL
- **THEN** the output SHALL contain streams mapped as `.Stream(0, 0, StreamType.Video, s => s.Copy())` and `.Stream(0, 0, StreamType.Audio, s => s.Copy())`
- **AND** the serialized arguments SHALL include `-map 0:v:0 -map 0:a:0 -c:v copy -c:a copy`

#### Scenario: HLS with timed_id3 track
- **WHEN** building arguments for an HLS URL containing a `timed_id3` data stream
- **THEN** only video and audio streams from input 0 SHALL be mapped
- **AND** the `timed_id3` stream SHALL not appear in the output

### Requirement: FFmpeg applies BSF tolerance for HLS audio
When `FfmpegInput.IsHls` is true, the output builder SHALL apply `-bsf:a aac_adtstoasc=no_validation=1` via `OutputBuilder.CustomArg`.

#### Scenario: HLS input gets BSF tolerance
- **WHEN** building arguments with `IsHls` true
- **THEN** the output SHALL include `CustomArg("-bsf:a aac_adtstoasc=no_validation=1")`

#### Scenario: Direct MP4 does not get BSF tolerance
- **WHEN** building arguments with `IsHls` false
- **THEN** no `-bsf:a` argument SHALL be present

### Requirement: FFmpeg uses typed codec selection via StreamBuilder
The argument builder SHALL use FFmpeg.Rx's `StreamBuilder.Copy()` for stream-copy codec selection instead of FFMpegCore's `CopyChannel`.

#### Scenario: Per-stream copy
- **WHEN** building arguments for any input
- **THEN** the builder SHALL use `.Stream(0, 0, StreamType.Video, s => s.Copy())` and `.Stream(0, 0, StreamType.Audio, s => s.Copy())`

### Requirement: FFmpeg argument builder for direct HTTP video
The system SHALL use FFmpeg.Rx's `Ffmpeg.Input(Uri)` and `PipelineBuilder.Output(path, ...)` to build arguments for direct HTTP video sources with explicit stream mapping and codec copy into MKV.

#### Scenario: Direct HTTP without subtitle
- **WHEN** a download is started for a direct MP4 URL and no subtitle
- **THEN** the builder SHALL create `Ffmpeg.Input(new Uri(videoUrl))` with user-agent custom arg, call `.WithProgressReporting()`, and `.Output(outputPath, o => ...)` with video and audio stream-copy

#### Scenario: Direct HTTP with subtitle
- **WHEN** a download is started for a direct MP4 URL and a subtitle path
- **THEN** the builder SHALL add a second input via `.Input(subtitlePath)` and add a subtitle stream `.Stream(1, StreamType.Subtitle, s => s.Encode(SubtitleCodec.Srt).Disposition("0").Metadata("language", lang))`

### Requirement: FFmpeg argument builder for HLS video
The system SHALL use FFmpeg.Rx's builder to build arguments for HLS sources with BSF tolerance.

#### Scenario: HLS without subtitle
- **WHEN** a download is started for an HLS URL and no subtitle
- **THEN** the builder SHALL include BSF tolerance via `CustomArg` in addition to video/audio stream-copy

#### Scenario: HLS with subtitle
- **WHEN** a download is started for an HLS URL and a subtitle path
- **THEN** the builder SHALL include both BSF tolerance and subtitle stream arguments

### Requirement: Single builder flow for all input types
The argument builder SHALL use a single code path with conditional additions for subtitle and HLS.

#### Scenario: No code duplication
- **WHEN** building arguments with or without subtitles
- **THEN** the builder SHALL use one `.Output(path, o => ...)` block with conditional stream additions inside it

### Requirement: FFmpeg progress reporting via Observable bridge
The system SHALL subscribe to `IFfmpegSession.Progress` (R3 Observable of `ProgressEvent`) and bridge each event to the `Action<ProgressUpdate>` callback by mapping `BytesWritten` → `TotalSize`, `TimeUs` → `OutTimeUs`, and `Speed` → `Speed`.

#### Scenario: Progress callback during download
- **WHEN** FFmpeg.Rx emits a `ProgressEvent` on `session.Progress`
- **THEN** the bridge SHALL invoke the progress callback with a `ProgressUpdate` containing the mapped field values

#### Scenario: Speed not available
- **WHEN** FFmpeg outputs `speed=N/A`
- **THEN** FFmpeg.Rx's `ProgressParser` SHALL report `Speed=0.0` and the bridge SHALL pass this through

### Requirement: FFmpeg process management via IFfmpegSession
The system SHALL use `IFfmpegSession.RunAsync(ct)` to execute FFmpeg and receive a typed `FFmpeg.Rx.Models.FfmpegResult`. The session SHALL be disposed via `await using`.

#### Scenario: Process exit success
- **WHEN** FFmpeg exits with code 0
- **THEN** `session.RunAsync(ct)` SHALL return `FfmpegResult(Success=true, ExitCode=0, Error=null, Duration=elapsed)`
- **AND** the mapper SHALL return FunkArr's `FfmpegResult(true, 0, null, (int)elapsed.TotalSeconds)`

#### Scenario: Process exit failure
- **WHEN** FFmpeg exits with a non-zero code
- **THEN** `session.RunAsync(ct)` SHALL return a result with extracted error and classified failure kind
- **AND** the mapper SHALL map `FFmpeg.Rx.FailureKind` to `FunkArr.Messages.Download.FailureKind` via exhaustive switch (Unknown→Permanent, Transient→Transient, Permanent→Permanent)

#### Scenario: Cancellation
- **WHEN** the CancellationToken is cancelled during execution
- **THEN** `session.RunAsync(ct)` SHALL return `FfmpegResult(Success=false, ExitCode=-1, Error="Cancelled")`
- **AND** the mapper SHALL return FunkArr's `FfmpegResult(false, -1, "Cancelled", elapsed)`

### Requirement: FFmpeg process as injectable service
The system SHALL provide `IFfmpegProcess` as a DI-injectable interface registered as transient. The implementation SHALL use FFmpeg.Rx internally but SHALL NOT expose any FFmpeg.Rx types through the interface.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `IFfmpegProcess` SHALL be registered as transient in `DownloadServiceExtensions`

#### Scenario: Type visibility
- **WHEN** FunkArr.Download is referenced by other projects
- **THEN** `FfmpegProcess` SHALL be internal; `IFfmpegProcess`, `FfmpegResult`, and `ProgressUpdate` SHALL be public
- **AND** no FFmpeg.Rx types SHALL appear in the public API

### Requirement: Input options for user-agent and proxy
The builder SHALL set the user-agent header via `InputOptionConfig(CustomArgs: ["-user_agent \"Mozilla/5.0\""])` on the URL input. When a proxy URL is present, it SHALL add `-http_proxy <url>` to the input custom args.

#### Scenario: User-agent always set
- **WHEN** building arguments for any URL input
- **THEN** the serialized arguments SHALL include `-user_agent "Mozilla/5.0"` before the `-i` flag

#### Scenario: Proxy set when provided
- **WHEN** building arguments with a non-null proxy URL
- **THEN** the serialized arguments SHALL include `-http_proxy <proxy>` before the `-i` flag

#### Scenario: No proxy when null
- **WHEN** building arguments with a null proxy URL
- **THEN** no `-http_proxy` argument SHALL be present

## REMOVED Requirements

### Requirement: FFmpeg uses fluent API for codec selection
**Reason**: Replaced by typed StreamBuilder codec selection via FFmpeg.Rx. FFMpegCore's `CopyChannel(Channel.Video)` / `CopyChannel(Channel.Audio)` no longer applies.
**Migration**: Use `.Stream(inputIndex, streamIndex, StreamType.Video, s => s.Copy())` pattern.

### Requirement: FFmpeg process management via FFMpegCore
**Reason**: FFMpegCore's `ProcessAsynchronously` and `FFMpegException` are replaced by FFmpeg.Rx's `IFfmpegSession.RunAsync()` and typed `FfmpegResult`. Error extraction and classification are now handled internally by FFmpeg.Rx.
**Migration**: Use `await session.RunAsync(ct)` which returns a typed result with error and failure kind already extracted.

### Requirement: FFmpeg runner as injectable service
**Reason**: Renamed to "FFmpeg process as injectable service" to match current interface name (`IFfmpegProcess` not `IFfmpegRunner`). The old requirement referenced `IFfmpegRunner` and `AddFfmpegRunner()` which no longer exist.
**Migration**: Use `IFfmpegProcess` interface and existing `DownloadServiceExtensions` registration.
