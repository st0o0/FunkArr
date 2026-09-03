## REMOVED Requirements

### Requirement: FfmpegProgress record
**Reason**: Replaced by `ProgressUpdate` message in the ffmpeg-runner capability. The `FfmpegProgress` intermediate record added unnecessary wrapping — its fields are now flattened directly into `ProgressUpdate`.
**Migration**: Parser returns `ProgressUpdate` directly or the runner maps internally. The `IsEnd` field is dropped as it was unused.
