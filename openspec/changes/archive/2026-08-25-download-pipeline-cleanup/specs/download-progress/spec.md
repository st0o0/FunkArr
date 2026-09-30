## ADDED Requirements

### Requirement: Worker actors send progress ticks
`Mp4DownloadActor` and `HlsDownloadActor` SHALL send `ProgressTick(nzoId, downloadedBytes, totalBytes)` messages to their parent DownloadActor periodically during download. Ticks SHALL be sent every 2 seconds or every 1MB downloaded, whichever comes first.

#### Scenario: Mp4 download progress
- **WHEN** `Mp4DownloadActor` is downloading a file with Content-Length 100MB
- **THEN** it SHALL send `ProgressTick` messages with `totalBytes = 104857600` and incrementing `downloadedBytes`

#### Scenario: HLS download progress
- **WHEN** `HlsDownloadActor` is downloading via FFmpeg
- **THEN** it SHALL parse FFmpeg progress output and send `ProgressTick` with duration-based byte estimates

#### Scenario: Unknown total size
- **WHEN** the HTTP response has no Content-Length header
- **THEN** `Mp4DownloadActor` SHALL send `ProgressTick` with `totalBytes = 0` (unknown)

### Requirement: DownloadActor forwards progress
DownloadActor SHALL forward `ProgressTick` messages from child workers to the DownloadRequestActor shard as `ReportProgress(nzoId, status, percentage, mb, mbleft)`. The percentage SHALL be calculated as `downloadedBytes * 100 / totalBytes` (0 if totalBytes is 0). `mb` and `mbleft` SHALL be in megabytes.

#### Scenario: Progress forwarded
- **WHEN** DownloadActor receives `ProgressTick("abc123", 52428800, 104857600)`
- **THEN** it SHALL tell DownloadRequestActor with `ReportProgress("abc123", "Downloading", 50, 100.0, 50.0)`

#### Scenario: Unknown total forwarded as zero
- **WHEN** DownloadActor receives `ProgressTick("abc123", 52428800, 0)`
- **THEN** it SHALL tell DownloadRequestActor with `ReportProgress("abc123", "Downloading", 0, 50.0, 0.0)`

### Requirement: DownloadRequestActor stores progress in-memory
DownloadRequestActor SHALL store `Percentage` (int), `Mb` (double), and `Mbleft` (double) in its state. These fields SHALL NOT be persisted — they are ephemeral and reset to defaults on recovery. The `QueryStatus` response SHALL include these fields.

#### Scenario: Progress available in status query
- **WHEN** `QueryStatus` is received after progress updates show 75% complete at 150MB/200MB
- **THEN** the response SHALL include `Percentage = 75`, `Mb = 200.0`, `Mbleft = 50.0`

#### Scenario: Progress defaults after recovery
- **WHEN** DownloadRequestActor recovers from a crash during an active download
- **THEN** `Percentage` SHALL be 0, `Mb` SHALL be 0.0, `Mbleft` SHALL be 0.0 until new ticks arrive
