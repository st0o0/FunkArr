## Purpose

HLS stream download via FFmpeg: URL type detection, FFmpeg process invocation with stream-copy, stderr progress parsing, and cancellation support for .m3u8 video sources.

## Requirements

### Requirement: HLS stream download via FFmpeg
The system SHALL download HLS video streams (.m3u8 URLs) by invoking FFmpeg as an external process with `-i "url" -map 0:v -map 0:a -c copy output.mp4`. The output SHALL be a regular MP4 file suitable for the remux stage.

#### Scenario: Successful HLS download
- **WHEN** a download request has a video URL ending in `.m3u8`
- **THEN** FFmpeg is invoked with the URL as input, stream-copies video and audio tracks to a temp MP4 file, and the stage produces a `VideoDownloadResult` with the temp file path

#### Scenario: HLS download fails
- **WHEN** FFmpeg exits with a non-zero exit code during HLS download (e.g. 403, network error, invalid manifest)
- **THEN** the stage produces a typed failure outcome with the FFmpeg stderr content as the error message, and the stream continues processing other downloads

#### Scenario: HLS download timeout
- **WHEN** an HLS download does not complete within the configured timeout
- **THEN** the FFmpeg process is killed and the stage produces a typed failure outcome

### Requirement: URL type detection
The system SHALL detect whether a video URL is a direct download (.mp4) or an HLS stream (.m3u8) and route to the appropriate download mechanism.

#### Scenario: MP4 URL detected
- **WHEN** a download request URL ends with `.mp4` or has Content-Type `video/mp4`
- **THEN** the request is routed to the direct HTTP download flow

#### Scenario: M3U8 URL detected
- **WHEN** a download request URL ends with `.m3u8` or has Content-Type `application/x-mpegURL`
- **THEN** the request is routed to the HLS download flow

#### Scenario: Unknown URL type defaults to direct download
- **WHEN** a download request URL does not match `.mp4` or `.m3u8` patterns
- **THEN** the request is routed to the direct HTTP download flow as a fallback

### Requirement: FFmpeg stderr progress parsing for HLS
The system SHALL parse FFmpeg's stderr output during HLS downloads to extract progress information. Progress SHALL be reported via the same callback mechanism as direct downloads.

#### Scenario: Progress extracted from FFmpeg output
- **WHEN** FFmpeg emits a stderr line containing `time=00:15:30.00` and `speed=2.5x` during an HLS download
- **THEN** the progress callback is invoked with elapsed duration 930 seconds and total duration from the download request metadata (or 0 if unknown)

#### Scenario: FFmpeg output does not contain time information
- **WHEN** FFmpeg stderr lines do not match the expected `time=` pattern
- **THEN** no progress callback is invoked and the download continues without progress reporting

#### Scenario: Total duration known from search metadata
- **WHEN** the download request includes duration metadata (e.g. 3600 seconds) and FFmpeg reports `time=00:30:00.00`
- **THEN** progress is reported as 1800 elapsed out of 3600 total, allowing percentage calculation

### Requirement: HLS download cancellation
The system SHALL support cancellation of in-progress HLS downloads by killing the FFmpeg process.

#### Scenario: Cancellation during HLS download
- **WHEN** the cancellation token is triggered while FFmpeg is downloading an HLS stream
- **THEN** the FFmpeg process is killed via `Process.Kill(entireProcessTree: true)` and the stage produces a failure outcome
