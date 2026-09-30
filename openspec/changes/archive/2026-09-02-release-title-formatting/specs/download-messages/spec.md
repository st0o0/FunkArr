## MODIFIED Requirements

### Requirement: AddDownload command

The system SHALL define an `AddDownload` record containing all metadata needed to start a download, extracted from the NZB file. The Title SHALL be the scene-style formatted release title.

#### Scenario: AddDownload fields

- **WHEN** an AddDownload message is created
- **THEN** it SHALL contain `Title` (string, scene-formatted), `VideoUrl` (string), `SubtitleUrl` (string?, nullable), `Channel` (string), `Duration` (int, seconds), `Size` (long, bytes), `Category` (string)

#### Scenario: Title is scene-formatted

- **WHEN** an AddDownload is created from a parsed NZB
- **THEN** the Title SHALL already be a scene-style string (e.g. `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`)
- **AND** the DownloadManager SHALL use this title directly for the output filename by appending `.mkv`
