# download-phase-progress Specification

## Purpose
Phase-aware progress calculation and API response formatting for downloads. The phase is tracked by the DownloadWorker directly instead of being derived client-side.
## Requirements
### Requirement: Phase-aware progress percentage
The system SHALL calculate download progress percentage based on the current phase. The phase SHALL be tracked by the DownloadWorker directly instead of being derived client-side.

#### Scenario: Download in progress shows byte-based percentage
- **WHEN** a download has `BytesDownloaded = 500MB` and `TotalBytes = 1000MB`
- **THEN** the percentage SHALL be `50`
- **AND** the phase SHALL be `DownloadPhase.VideoDownload`

#### Scenario: Download complete but remux in progress shows time-based percentage
- **WHEN** a download has `BytesDownloaded = 1000MB`, `TotalBytes = 1000MB`, `CurrentTimeUs = 1_800_000_000` (30 min), and `TotalDuration = 3600` (60 min)
- **THEN** the percentage SHALL be `50`
- **AND** the phase SHALL be `DownloadPhase.Remuxing`

#### Scenario: Transitional moment between download and remux
- **WHEN** a download has `BytesDownloaded >= TotalBytes` and `CurrentTimeUs == 0`
- **THEN** the phase SHALL be `DownloadPhase.VideoDownload`
- **AND** the percentage SHALL be `100`

### Requirement: Phase field in internal API response
The internal download queue API (`/api/downloads/queue`) SHALL include a `phase` string field on each queue item with the full phase name from `DownloadPhase` enum.

#### Scenario: Internal API returns phase for active download
- **WHEN** the client requests `GET /api/downloads/queue`
- **THEN** each item in the response SHALL include a `phase` field
- **AND** the `phase` value SHALL be the lowercase enum name (e.g., `"videoDownload"`, `"remuxing"`, `"subtitleDownload"`)

### Requirement: SABnzbd API percentage reflects current phase
The SABnzbd download API (`/download/api?mode=queue`) SHALL use the phase-aware percentage calculation. The `Status` field SHALL remain `"Downloading"` for all active phases to maintain compatibility with Sonarr/Radarr.

#### Scenario: SABnzbd queue shows byte-based percentage during download
- **WHEN** Sonarr queries `/download/api?mode=queue&output=json`
- **AND** a download is in the VideoDownload phase
- **THEN** the `percentage` field SHALL reflect bytes downloaded vs total bytes

#### Scenario: SABnzbd queue shows time-based percentage during remux
- **WHEN** Sonarr queries `/download/api?mode=queue&output=json`
- **AND** a download is in the Remuxing phase
- **THEN** the `percentage` field SHALL reflect FFmpeg time progress

