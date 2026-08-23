## Purpose

Centralized file path construction, directory initialization, temp file cleanup, and subtitle writing via `IFileService`.

## Requirements

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

### Requirement: Directory initialization
The system SHALL ensure required directories exist before file operations begin.

#### Scenario: Directories created on startup
- **WHEN** the download queue actor starts and the temp/download directories do not exist
- **THEN** `IFileService.EnsureDirectoriesExist` SHALL create both directories

#### Scenario: Output directory created before muxing
- **WHEN** `MuxingService` prepares to write an output file
- **THEN** the output subdirectory (`{downloadPath}/{title}/`) SHALL be created if it does not exist

### Requirement: Temp file cleanup
The system SHALL provide safe cleanup of temp files after successful muxing.

#### Scenario: Successful cleanup
- **WHEN** `CleanupTempFiles` is called with existing video and subtitle paths
- **THEN** both files SHALL be deleted

#### Scenario: Cleanup tolerates missing files
- **WHEN** `CleanupTempFiles` is called with a path to a file that no longer exists
- **THEN** the operation SHALL complete without throwing an exception

### Requirement: Subtitle file writing
The system SHALL provide a method to write subtitle content to disk during the download phase.

#### Scenario: Write subtitle bytes
- **WHEN** `WriteSubtitleAsync` is called with a path and byte content
- **THEN** the content SHALL be written to the specified path
