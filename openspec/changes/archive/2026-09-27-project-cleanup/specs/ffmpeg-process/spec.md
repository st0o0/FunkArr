## ADDED Requirements

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

## MODIFIED Requirements

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
