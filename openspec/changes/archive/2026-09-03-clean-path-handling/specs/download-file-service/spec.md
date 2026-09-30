## REMOVED Requirements

### Requirement: IDownloadFileService interface
**Reason**: Replaced by `IDownloadFileOperations` (I/O only) and `DownloadPaths` (path computation).
**Migration**: Replace all `IDownloadFileService` references with `IDownloadFileOperations`. Path computation moves to `DownloadPaths.Compute()`.

### Requirement: DownloadFileService implementation
**Reason**: Replaced by `DownloadFileOperations` implementing `IDownloadFileOperations`.
**Migration**: Replace DI registration of `IDownloadFileService` with `IDownloadFileOperations`.

### Requirement: EnsureIncompletePath creates directory and returns temp file path
**Reason**: Path computation moves to `DownloadPaths.Compute()`. Directory creation moves to `IDownloadFileOperations.EnsureDirectory()`.
**Migration**: Call `DownloadPaths.Compute()` for the path, then `IDownloadFileOperations.EnsureDirectory()` for directory creation.

### Requirement: ResolveOutputPath computes category-routed complete path with collision safety
**Reason**: Path computation moves to `DownloadPaths.Compute()`.
**Migration**: Use `DownloadPaths.Compute().CompletePath` instead.

### Requirement: Episode identifier detection
**Reason**: Moves to `DownloadPaths` value object.
**Migration**: Episode detection is internal to `DownloadPaths.Compute()`.

### Requirement: MoveToComplete moves file and creates output directory
**Reason**: Split into `IDownloadFileOperations.EnsureDirectory()` + `IDownloadFileOperations.MoveFile()`.
**Migration**: Call `EnsureDirectory(Path.GetDirectoryName(outputPath))` then `MoveFile(src, dst)`.

### Requirement: CleanupIncomplete removes entity working directory
**Reason**: Moves to `IDownloadFileOperations.DeleteDirectory()`.
**Migration**: Call `DeleteDirectory(path)` with logging handled by the caller or the implementation.

## ADDED Requirements

### Requirement: IDownloadFileOperations interface
The system SHALL define an `IDownloadFileOperations` interface in `FunkArr.Download` with methods for directory creation, file moves, and directory deletion. This interface SHALL contain no path computation logic. This is a narrow, domain-scoped custom interface — not `System.IO.Abstractions` (rejected: full `System.IO` surface overkill for 3 operations) and not `IFileProvider` (read-only, no write/move/delete).

#### Scenario: Interface definition
- **WHEN** the `IDownloadFileOperations` interface is inspected
- **THEN** it SHALL declare `EnsureDirectory(string path)` returning `void`
- **AND** `MoveFile(string sourcePath, string destinationPath)` returning `void`
- **AND** `DeleteDirectory(string path)` returning `void`

### Requirement: DownloadFileOperations implementation
The system SHALL provide a `DownloadFileOperations` sealed class implementing `IDownloadFileOperations`, registered as a singleton.

#### Scenario: EnsureDirectory creates directory
- **WHEN** `EnsureDirectory("/downloads/complete/tv/Show.S01E01")` is called
- **THEN** the directory SHALL be created if it does not exist (idempotent)

#### Scenario: MoveFile moves with overwrite
- **WHEN** `MoveFile(source, destination)` is called
- **THEN** the file SHALL be moved to the destination, overwriting if it exists

#### Scenario: DeleteDirectory removes recursively with error swallowing
- **WHEN** `DeleteDirectory("/downloads/incomplete/abc-123")` is called
- **AND** the directory exists
- **THEN** the directory SHALL be deleted recursively

#### Scenario: DeleteDirectory on non-existent path
- **WHEN** `DeleteDirectory("/downloads/incomplete/abc-123")` is called
- **AND** the directory does not exist
- **THEN** no error SHALL be thrown

#### Scenario: DeleteDirectory failure is non-fatal
- **WHEN** `DeleteDirectory` fails (e.g., file locked)
- **THEN** no exception SHALL be thrown (failure is swallowed)

### Requirement: EnsureDirectories on startup
The system SHALL ensure base directories (`CompletePath`, `IncompletePath`, and category subdirectories) exist at startup via `IHostedLifecycleService`, using `IDownloadFileOperations.EnsureDirectory()`.

#### Scenario: Startup bootstrapping
- **WHEN** the application starts
- **THEN** `CompletePath`, `IncompletePath`, and all configured category subdirectories SHALL be created
