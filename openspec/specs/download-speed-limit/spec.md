# download-speed-limit Specification

## Purpose
TBD - created by archiving change download-scheduling-and-limits. Update Purpose after archive.
## Requirements
### Requirement: FfmpegRunner applies speed limit when configured
The FfmpegRunner SHALL accept an optional speed limit parameter (bytes per second) and apply it as an FFmpeg input-level rate option when provided. When null or zero, no rate limiting SHALL be applied.

#### Scenario: Speed limit configured
- **WHEN** `SpeedLimitBytesPerSecond` is 10485760 (10 MB/s)
- **THEN** FfmpegRunner SHALL pass a rate-limiting option to FFmpeg on the input URL
- **AND** FFmpeg SHALL limit download speed to approximately 10 MB/s

#### Scenario: No speed limit
- **WHEN** `SpeedLimitBytesPerSecond` is null or 0
- **THEN** FfmpegRunner SHALL NOT add any rate-limiting arguments
- **AND** behavior SHALL be identical to current implementation

#### Scenario: Speed limit is per-worker
- **WHEN** 3 concurrent downloads are running with a 10 MB/s limit
- **THEN** each worker SHALL independently limit to 10 MB/s
- **AND** total bandwidth MAY reach up to 30 MB/s

### Requirement: DownloadWorker passes speed limit to FfmpegRunner
The DownloadWorker SHALL read `SpeedLimitBytesPerSecond` from `DownloadOptions` and pass it to `FfmpegRunner.Run()`.

#### Scenario: Worker provides speed limit
- **WHEN** a DownloadWorker starts a download
- **THEN** it SHALL read the current `SpeedLimitBytesPerSecond` from options
- **AND** pass it to the FfmpegRunner

