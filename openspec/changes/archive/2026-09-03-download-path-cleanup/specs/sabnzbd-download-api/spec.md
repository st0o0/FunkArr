## MODIFIED Requirements

### Requirement: History response returns directory path as storage
The SABnzbd history endpoint SHALL return the parent directory of the completed download file as the `storage` field, not the file path itself. Sonarr/Radarr scan this directory for video files to import.

#### Scenario: Completed download storage path
- **WHEN** the history endpoint builds a `HistorySlot` for a completed download
- **AND** the internal `FilePath` is `"/downloads/complete/tv/Show.S01E01/Show.S01E01.mkv"`
- **THEN** the `storage` field SHALL be `"/downloads/complete/tv/Show.S01E01"`

#### Scenario: Failed download storage path
- **WHEN** the history endpoint builds a `HistorySlot` for a failed download
- **AND** `FilePath` is null
- **THEN** the `storage` field SHALL be null
