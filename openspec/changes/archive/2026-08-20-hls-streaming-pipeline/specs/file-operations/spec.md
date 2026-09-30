## MODIFIED Requirements

### Requirement: Centralized file path construction
The system SHALL provide an `IFileService` interface that centralizes all file path construction for temp files and output files. Subtitle temp paths SHALL preserve the original format extension for proper content-based format detection.

#### Scenario: Temp video path
- **WHEN** `GetTempVideoPath` is called with tempPath `data/temp` and nzoId `abc123`
- **THEN** the result SHALL be `data/temp/abc123.mp4`

#### Scenario: Temp subtitle path with format extension
- **WHEN** `GetTempSubtitlePath` is called with tempPath `data/temp`, nzoId `abc123`, and extension `.vtt`
- **THEN** the result SHALL be `data/temp/abc123.vtt`

#### Scenario: Temp subtitle path with default extension
- **WHEN** `GetTempSubtitlePath` is called with tempPath `data/temp`, nzoId `abc123`, and no extension specified
- **THEN** the result SHALL be `data/temp/abc123.sub` (generic extension, triggers content-based sniffing in normalize stage)

#### Scenario: Normalized subtitle path
- **WHEN** `GetNormalizedSubtitlePath` is called with tempPath `data/temp` and nzoId `abc123`
- **THEN** the result SHALL be `data/temp/abc123.srt` (always SRT, used after normalization)

#### Scenario: Output path
- **WHEN** `GetOutputPath` is called with downloadPath `/media/downloads` and title `My Show S01E03`
- **THEN** the result SHALL be `/media/downloads/My Show S01E03/My Show S01E03.mkv`
