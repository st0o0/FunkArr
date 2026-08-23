## Purpose

Subtitle acquisition, content-based format sniffing, and normalization to SRT as dedicated Akka Streams pipeline stages, decoupled from both the video download and remux stages.

## Requirements

### Requirement: Subtitle acquisition as dedicated stream stage
The system SHALL acquire subtitles in a dedicated Akka Streams stage, separate from the video download stage. The stage SHALL handle three acquisition paths based on available information.

#### Scenario: Subtitle from separate URL
- **WHEN** the download request has a non-null `SubtitleUrl`
- **THEN** the stage downloads the subtitle via HTTP GET and produces a result with the subtitle file path

#### Scenario: Subtitle from HLS manifest (fallback)
- **WHEN** the download request has no `SubtitleUrl` and the video source was HLS
- **THEN** the stage runs `ffprobe -v quiet -print_format json -show_streams` on the original .m3u8 URL, checks for streams with `codec_type=subtitle`, and if found extracts the first subtitle track via `ffmpeg -i url.m3u8 -map 0:s:0 -c:s srt output.srt`

#### Scenario: No subtitle in HLS manifest
- **WHEN** the download request has no `SubtitleUrl`, the video source was HLS, and ffprobe finds no subtitle streams in the manifest
- **THEN** the stage produces a result with null subtitle path and the pipeline continues without subtitles

#### Scenario: No subtitle available
- **WHEN** the download request has no `SubtitleUrl` and the video source was not HLS
- **THEN** the stage produces a result with null subtitle path

#### Scenario: Subtitle download fails gracefully
- **WHEN** the subtitle HTTP GET returns a non-success status code or ffprobe/ffmpeg fails
- **THEN** the stage logs a warning and produces a result with null subtitle path, the pipeline continues without subtitles

### Requirement: Content-based subtitle format sniffing
The system SHALL detect subtitle format by inspecting file content rather than file extension. Format detection SHALL read the first 512 bytes of the subtitle file.

#### Scenario: WebVTT detected by content
- **WHEN** the subtitle file content starts with `WEBVTT`
- **THEN** the format is detected as WebVTT

#### Scenario: TTML detected by content
- **WHEN** the subtitle file content starts with `<?xml` or contains `<tt` within the first 512 bytes
- **THEN** the format is detected as TTML/EBU-TT

#### Scenario: SRT detected by content
- **WHEN** the subtitle file content contains lines matching the pattern `digits CRLF/LF timestamp --> timestamp`
- **THEN** the format is detected as SRT

#### Scenario: Unknown format defaults to SRT
- **WHEN** the subtitle file content does not match any known format pattern
- **THEN** the format is treated as SRT (best effort passthrough)

### Requirement: Subtitle normalization as dedicated stream stage
The system SHALL normalize subtitle files to SRT format in a dedicated Akka Streams stage, separate from the remux stage.

#### Scenario: WebVTT normalized to SRT
- **WHEN** the subtitle format is detected as WebVTT
- **THEN** the stage converts it to SRT (removing WEBVTT header, STYLE/NOTE blocks, converting `.` millisecond separators to `,`, adding sequential cue numbers) and writes the result to a new file

#### Scenario: TTML normalized to SRT
- **WHEN** the subtitle format is detected as TTML/EBU-TT
- **THEN** the stage converts it to SRT using the existing TTML-to-SRT conversion logic and writes the result to a new file

#### Scenario: SRT passes through unchanged
- **WHEN** the subtitle format is detected as SRT
- **THEN** the file is used as-is without conversion

#### Scenario: No subtitle skips normalization
- **WHEN** the subtitle path is null
- **THEN** the stage passes through the result unchanged
