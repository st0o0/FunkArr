# download-phase-progress Specification

## Purpose
TBD - created by archiving change download-phase-indicator. Update Purpose after archive.
## Requirements
### Requirement: Phase-aware progress percentage

The system SHALL calculate download progress percentage based on the current phase:
- During the **downloading** phase: `percentage = BytesDownloaded * 100 / TotalBytes`
- During the **remuxing** phase: `percentage = CurrentTimeUs / 1_000_000 / TotalDuration * 100`

The phase SHALL be derived from existing state:
- **Downloading**: `BytesDownloaded < TotalBytes`, or `BytesDownloaded >= TotalBytes` and `CurrentTimeUs == 0`
- **Remuxing**: `BytesDownloaded >= TotalBytes` and `CurrentTimeUs > 0`

#### Scenario: Download in progress shows byte-based percentage
- **WHEN** a download has `BytesDownloaded = 500MB` and `TotalBytes = 1000MB`
- **THEN** the percentage SHALL be `50`
- **AND** the phase SHALL be `"downloading"`

#### Scenario: Download complete but remux in progress shows time-based percentage
- **WHEN** a download has `BytesDownloaded = 1000MB`, `TotalBytes = 1000MB`, `CurrentTimeUs = 1_800_000_000` (30 min), and `TotalDuration = 3600` (60 min)
- **THEN** the percentage SHALL be `50`
- **AND** the phase SHALL be `"remuxing"`

#### Scenario: Transitional moment between download and remux
- **WHEN** a download has `BytesDownloaded >= TotalBytes` and `CurrentTimeUs == 0`
- **THEN** the phase SHALL be `"downloading"`
- **AND** the percentage SHALL be `100`

### Requirement: Phase field in internal API response

The internal download queue API (`/api/downloads/queue`) SHALL include a `phase` string field on each queue item with value `"downloading"` or `"remuxing"`.

#### Scenario: Internal API returns phase for active download
- **WHEN** the client requests `GET /api/downloads/queue`
- **THEN** each item in the response SHALL include a `phase` field
- **AND** the `phase` value SHALL be `"downloading"` or `"remuxing"`

### Requirement: SABnzbd API percentage reflects current phase

The SABnzbd download API (`/download/api?mode=queue`) SHALL use the phase-aware percentage calculation. The `Status` field SHALL remain `"Downloading"` for both phases to maintain compatibility with Sonarr/Radarr.

#### Scenario: SABnzbd queue shows byte-based percentage during download
- **WHEN** Sonarr queries `/download/api?mode=queue&output=json`
- **AND** a download is in the downloading phase
- **THEN** the `percentage` field SHALL reflect bytes downloaded vs total bytes

#### Scenario: SABnzbd queue shows time-based percentage during remux
- **WHEN** Sonarr queries `/download/api?mode=queue&output=json`
- **AND** a download is in the remuxing phase
- **THEN** the `percentage` field SHALL reflect FFmpeg time progress

