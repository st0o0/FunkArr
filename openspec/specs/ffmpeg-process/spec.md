# FFmpeg Process

## Purpose

FFmpeg process spawning, argument building (direct/HLS, with/without subtitle), and machine-readable progress parsing.

## Requirements

### Requirement: FFmpeg argument builder for direct HTTP video
The system SHALL build FFmpeg arguments for downloading direct HTTP video sources (.mp4) with stream-copy into MKV format.

#### Scenario: Direct HTTP without subtitle
- **WHEN** arguments are built for VideoUrl "https://example.com/video.mp4" and no subtitle, with OutputPath "/downloads/output.mkv"
- **THEN** the arguments SHALL be: `-i "https://example.com/video.mp4" -c copy -progress pipe:1 "/downloads/output.mkv"`

#### Scenario: Direct HTTP with subtitle
- **WHEN** arguments are built for VideoUrl "https://example.com/video.mp4" and SubtitleUrl "https://example.com/subs.ttml", with OutputPath "/downloads/output.mkv"
- **THEN** the arguments SHALL be: `-i "https://example.com/video.mp4" -i "https://example.com/subs.ttml" -c:v copy -c:a copy -c:s srt -metadata:s:s:0 language=deu -progress pipe:1 "/downloads/output.mkv"`

### Requirement: FFmpeg argument builder for HLS video
The system SHALL build FFmpeg arguments for downloading HLS video sources (.m3u8) identically to direct HTTP — FFmpeg handles the protocol transparently.

#### Scenario: HLS without subtitle
- **WHEN** arguments are built for VideoUrl "https://apasfiis.sf.apa.at/.../chunklist.m3u8" and no subtitle
- **THEN** the arguments SHALL be: `-i "https://apasfiis.sf.apa.at/.../chunklist.m3u8" -c copy -progress pipe:1 "/downloads/output.mkv"`

#### Scenario: HLS with subtitle
- **WHEN** arguments are built for an HLS VideoUrl and a SubtitleUrl
- **THEN** the arguments SHALL include both `-i` inputs and `-c:s srt` for subtitle conversion, identical structure to direct HTTP with subtitle

### Requirement: FFmpeg progress parser
The system SHALL parse FFmpeg's machine-readable progress output (`-progress pipe:1`) into structured data.

#### Scenario: Parse complete progress block
- **WHEN** FFmpeg outputs a progress block containing `out_time_us=11360000`, `total_size=11010048`, `speed=1.5x`, `progress=continue`
- **THEN** the parser SHALL produce CurrentTimeUs=11360000, BytesDownloaded=11010048, Speed=1.5

#### Scenario: Parse final progress block
- **WHEN** FFmpeg outputs a progress block containing `progress=end`
- **THEN** the parser SHALL produce a final progress entry and signal completion

#### Scenario: Parse speed without multiplier
- **WHEN** FFmpeg outputs `speed=N/A` (e.g., at start of stream)
- **THEN** the parser SHALL use Speed=0.0

### Requirement: FFmpeg process management
The system SHALL spawn FFmpeg as an external OS process and manage its lifecycle.

#### Scenario: Process spawn
- **WHEN** a download is started
- **THEN** the system SHALL spawn `ffmpeg` with the built arguments, redirect stdout for progress reading, and capture stderr for error diagnostics

#### Scenario: Process exit success
- **WHEN** the FFmpeg process exits with code 0
- **THEN** the system SHALL signal completion and provide the output file path

#### Scenario: Process exit failure
- **WHEN** the FFmpeg process exits with a non-zero code
- **THEN** the system SHALL signal failure and provide the last stderr output as the error reason

### Requirement: FFmpeg overwrite handling
The system SHALL pass `-y` to FFmpeg to automatically overwrite output files without prompting, preventing the process from blocking on interactive input.

#### Scenario: Overwrite flag
- **WHEN** FFmpeg arguments are built
- **THEN** `-y` SHALL be included before any input arguments
