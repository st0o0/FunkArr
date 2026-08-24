## Purpose

Centralized file path construction, directory initialization, temp file cleanup, managed file writing, and subtitle normalization via `IFileService`. Uses `IFileSystem` from `System.IO.Abstractions` for all filesystem operations to enable testability.

## Requirements

### Requirement: Centralized file path construction
The system SHALL provide an `IFileService` interface that centralizes all file path construction for temp files and output files. `IFileService` SHALL receive root paths (`TempPath`, `Path`) and category configuration (`Category` dictionary) via `IOptions<DownloadOptions>` at construction time. Temp path methods SHALL require only identity parameters (`nzoId`). Output path methods SHALL accept `title` and an optional `category` parameter, resolving category to output directory internally via `CategoryResolver`. The implementation SHALL use `IFileSystem` from `System.IO.Abstractions` for all filesystem operations.

#### Scenario: Temp video path
- **WHEN** `GetTempVideoPath` is called with nzoId `abc123`
- **THEN** the result SHALL be `{TempPath}/abc123.mp4` where `TempPath` comes from `DownloadOptions`

#### Scenario: Temp subtitle path with format extension
- **WHEN** `GetTempSubtitlePath` is called with nzoId `abc123` and extension `.vtt`
- **THEN** the result SHALL be `{TempPath}/abc123.vtt`

#### Scenario: Temp subtitle path with default extension
- **WHEN** `GetTempSubtitlePath` is called with nzoId `abc123` and no extension specified
- **THEN** the result SHALL be `{TempPath}/abc123.sub`

#### Scenario: Normalized subtitle path
- **WHEN** `GetNormalizedSubtitlePath` is called with nzoId `abc123`
- **THEN** the result SHALL be `{TempPath}/abc123.srt`

#### Scenario: Output path without category
- **WHEN** `GetOutputPath` is called with title `My Show S01E03` and no category
- **THEN** the result SHALL be `{Path}/My Show S01E03/My Show S01E03.mkv`

#### Scenario: Output path with configured category
- **WHEN** `GetOutputPath` is called with title `My Show S01E03` and category `"tv"`, and `DownloadOptions.Category` contains `{"tv": "serien"}`, and `DownloadOptions.Path` is `/downloads`
- **THEN** the result SHALL be `/downloads/serien/My Show S01E03/My Show S01E03.mkv`

#### Scenario: Output path with unknown category fallback
- **WHEN** `GetOutputPath` is called with title `My Show S01E03` and category `"anime"`, and no configured override exists, and `DownloadOptions.Path` is `/downloads`
- **THEN** the result SHALL be `/downloads/anime/My Show S01E03/My Show S01E03.mkv`

### Requirement: Directory initialization
The system SHALL ensure required directories exist before file operations begin. `EnsureDirectoriesExist` SHALL take no parameters and SHALL create both `TempPath` and `DownloadPath` from the injected configuration.

#### Scenario: Directories created on startup
- **WHEN** the download queue actor starts and the temp/download directories do not exist
- **THEN** `IFileService.EnsureDirectoriesExist()` SHALL create both directories using the paths from `DownloadOptions`

#### Scenario: Output directory created before muxing
- **WHEN** `EnsureOutputDirectory` is called with a title and optional category
- **THEN** the output subdirectory (resolved via category routing, then `/{title}/`) SHALL be created if it does not exist

### Requirement: Temp file cleanup
The system SHALL provide cleanup of all temp files for a given download by nzoId. `CleanupTemp(string nzoId)` SHALL delete the temp video file and any temp subtitle files (all known extensions) associated with the nzoId.

#### Scenario: Cleanup by nzoId
- **WHEN** `CleanupTemp` is called with nzoId `abc123`
- **THEN** all temp files matching `{TempPath}/abc123.*` SHALL be deleted

#### Scenario: Cleanup tolerates missing files
- **WHEN** `CleanupTemp` is called and some expected temp files do not exist
- **THEN** the operation SHALL complete without throwing an exception

### Requirement: Managed video file writing
`IFileService.SaveVideoAsync(string nzoId, Stream content)` SHALL write the given stream content to the temp video path for the nzoId. The write SHALL use an 8192-byte buffer and create the file with `FileMode.Create`.

#### Scenario: Save video from HTTP stream
- **WHEN** `SaveVideoAsync` is called with nzoId `abc123` and an HTTP response stream
- **THEN** the content SHALL be written to `{TempPath}/abc123.mp4` in 8192-byte chunks

### Requirement: Managed subtitle file writing
`IFileService.SaveSubtitleAsync(string nzoId, byte[] content, string extension)` SHALL write the given byte content to the temp subtitle path for the nzoId with the specified extension.

#### Scenario: Save subtitle bytes
- **WHEN** `SaveSubtitleAsync` is called with nzoId `abc123`, subtitle bytes, and extension `.vtt`
- **THEN** the content SHALL be written to `{TempPath}/abc123.vtt`

### Requirement: Managed subtitle normalization
`IFileService.NormalizeSubtitleAsync(string nzoId)` SHALL resolve the temp subtitle path and normalized output path from the nzoId, then delegate to `SubtitleNormalizer.NormalizeAsync` for format conversion. The method SHALL return the normalized path on success or `null` on failure.

#### Scenario: VTT subtitle normalized to SRT
- **WHEN** `NormalizeSubtitleAsync` is called with nzoId `abc123` and a VTT file exists at `{TempPath}/abc123.vtt`
- **THEN** `SubtitleNormalizer.NormalizeAsync` SHALL be called with the VTT input path and SRT output path, and the method SHALL return the SRT path

#### Scenario: No subtitle file found
- **WHEN** `NormalizeSubtitleAsync` is called and no temp subtitle file exists for the nzoId
- **THEN** the method SHALL return `null`

### Requirement: IFileSystem backing
The `FileService` implementation SHALL use `IFileSystem` from the `System.IO.Abstractions` NuGet package for all filesystem operations (`File.WriteAllBytesAsync`, `Directory.CreateDirectory`, `File.Delete`, `Path.Combine`, etc.). This enables testing with `MockFileSystem` without touching the real filesystem.

#### Scenario: FileService constructed with MockFileSystem in tests
- **WHEN** a test creates `FileService` with a `MockFileSystem` and `IOptions<DownloadOptions>`
- **THEN** all file operations SHALL use the mock filesystem and no real files SHALL be created
