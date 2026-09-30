# ffmpeg-runner (delta)

## MODIFIED Requirements

### Requirement: Error reporting extracts meaningful message from stderr

FfmpegRunner SHALL extract a human-readable error message from FFmpeg's stderr output instead of storing the raw output. The extraction SHALL look for known error patterns and return the most specific match.

#### Scenario: HTTP 403 error

- **WHEN** FFmpeg stderr contains "HTTP error 403 Forbidden"
- **THEN** the fail message SHALL be "HTTP error 403 Forbidden" (not the full stderr with version banner)

#### Scenario: Error opening input file

- **WHEN** FFmpeg stderr contains "Error opening input file https://example.com/video.mp4"
- **THEN** the fail message SHALL contain "Error opening input file" and the URL

#### Scenario: Server returned error

- **WHEN** FFmpeg stderr contains "Server returned 403 Forbidden (access denied)"
- **THEN** the fail message SHALL be "Server returned 403 Forbidden (access denied)"

#### Scenario: Unknown error pattern

- **WHEN** FFmpeg stderr does not match any known error pattern
- **THEN** the fail message SHALL be the last non-empty line of stderr

#### Scenario: Empty stderr

- **WHEN** FFmpeg stderr is null or empty
- **THEN** the fail message SHALL be an empty string
