## MODIFIED Requirements

### Requirement: DownloadWorker reports subtitle status accurately
The WorkerStatusResult.HasSubtitles field SHALL reflect whether a subtitle track was actually muxed into the output file, not whether a SubtitleUrl was present in the download metadata.

#### Scenario: Subtitle download succeeded
- **WHEN** a download completes and the subtitle was successfully downloaded and muxed
- **THEN** WorkerStatusResult.HasSubtitles SHALL be true

#### Scenario: Subtitle download failed
- **WHEN** a download completes but the subtitle download failed (HTTP error, parse error, empty content)
- **AND** the video was muxed without a subtitle track
- **THEN** WorkerStatusResult.HasSubtitles SHALL be false

#### Scenario: No subtitle URL
- **WHEN** a download completes and SubtitleUrl was null
- **THEN** WorkerStatusResult.HasSubtitles SHALL be false

#### Scenario: Recovery default
- **WHEN** a DownloadWorker recovers from persisted state
- **THEN** HasSubtitles SHALL default to false (conservative — subtitle outcome is not persisted)

### Requirement: Subtitle cancellation does not propagate as Permanent failure
When the subtitle download is cancelled via CancellationToken, the Remuxer SHALL return a cancellation result rather than letting OperationCanceledException escape as a Permanent FfmpegResult failure.

#### Scenario: Subtitle download cancelled
- **WHEN** the subtitle download is in progress
- **AND** CancelDownload is received (CancellationToken fires)
- **THEN** the Remuxer SHALL catch OperationCanceledException from the subtitle phase
- **AND** return an FfmpegResult indicating cancellation (not Permanent failure)
- **AND** the Worker SHALL passivate without recording a Failed history entry

#### Scenario: Video download cancelled
- **WHEN** the FFmpeg video process is cancelled
- **THEN** the existing cancellation behavior SHALL be unchanged
