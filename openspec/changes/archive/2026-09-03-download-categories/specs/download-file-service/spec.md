## ADDED Requirements

### Requirement: IDownloadFileService interface
The system SHALL define an `IDownloadFileService` interface in `FunkArr.Download` with methods for path resolution, directory management, file moves, and cleanup.

#### Scenario: Interface definition
- **WHEN** the `IDownloadFileService` interface is inspected
- **THEN** it SHALL declare `ResolveTempPath(string entityId, string title)` returning `string`
- **AND** `ResolveOutputPath(string title, string? category)` returning `string`
- **AND** `MoveToComplete(string tempPath, string outputPath)` returning `void`
- **AND** `CleanupIncomplete(string entityId)` returning `void`
- **AND** `EnsureDirectories()` returning `void`

### Requirement: DownloadFileService implementation
The system SHALL provide a `DownloadFileService` sealed class in `FunkArr.Download` implementing `IDownloadFileService`, injecting `IOptions<DownloadOptions>`.

#### Scenario: DI registration
- **WHEN** the service collection is configured
- **THEN** `IDownloadFileService` SHALL be registered as a singleton mapping to `DownloadFileService`

### Requirement: ResolveTempPath computes incomplete working directory
The `ResolveTempPath` method SHALL return the full path to the temporary MKV file inside the incomplete directory for a given entity.

#### Scenario: Temp path resolution
- **WHEN** `ResolveTempPath("abc-123", "My.Show.S01E01")` is called
- **AND** `IncompletePath` is `"/downloads/incomplete"`
- **THEN** the result SHALL be the full path of `"/downloads/incomplete/abc-123/My.Show.S01E01.mkv"`

### Requirement: ResolveOutputPath computes category-routed complete path
The `ResolveOutputPath` method SHALL return the full path to the final MKV file under the complete directory, routed into a category subdirectory when the category resolves.

#### Scenario: Output path with known category
- **WHEN** `ResolveOutputPath("My.Show.S01E01", "tv")` is called
- **AND** category `"tv"` resolves to directory `"tv"`
- **AND** `CompletePath` is `"/downloads/complete"`
- **THEN** the result SHALL be the full path of `"/downloads/complete/tv/My.Show.S01E01/My.Show.S01E01.mkv"`

#### Scenario: Output path with custom category dir
- **WHEN** `ResolveOutputPath("My.Show.S01E01", "dokus")` is called
- **AND** category `"dokus"` resolves to directory `"dokumentationen"`
- **THEN** the result SHALL include `"dokumentationen"` as the category subdirectory

#### Scenario: Output path with unknown category
- **WHEN** `ResolveOutputPath("My.Show.S01E01", "unknown")` is called
- **AND** no matching category exists
- **THEN** the result SHALL be the full path of `"/downloads/complete/My.Show.S01E01/My.Show.S01E01.mkv"` (no category subdirectory)

#### Scenario: Output path with null category
- **WHEN** `ResolveOutputPath("My.Show.S01E01", null)` is called
- **THEN** the result SHALL be the full path of `"/downloads/complete/My.Show.S01E01/My.Show.S01E01.mkv"` (no category subdirectory)

### Requirement: MoveToComplete moves file and creates output directory
The `MoveToComplete` method SHALL ensure the output directory exists and move the temp file to the output path, overwriting if it exists.

#### Scenario: Move with directory creation
- **WHEN** `MoveToComplete("/incomplete/abc/title.mkv", "/complete/tv/title/title.mkv")` is called
- **AND** the directory `"/complete/tv/title"` does not exist
- **THEN** the directory SHALL be created
- **AND** the file SHALL be moved to the output path

#### Scenario: Move with overwrite
- **WHEN** `MoveToComplete(tempPath, outputPath)` is called
- **AND** a file already exists at `outputPath`
- **THEN** the existing file SHALL be overwritten

### Requirement: CleanupIncomplete removes entity working directory
The `CleanupIncomplete` method SHALL delete the incomplete directory for a given entity ID recursively. Failure SHALL be logged as a warning but SHALL NOT throw.

#### Scenario: Successful cleanup
- **WHEN** `CleanupIncomplete("abc-123")` is called
- **AND** the directory `"/downloads/incomplete/abc-123"` exists
- **THEN** the directory SHALL be deleted recursively

#### Scenario: Directory does not exist
- **WHEN** `CleanupIncomplete("abc-123")` is called
- **AND** the directory does not exist
- **THEN** no error SHALL be thrown

#### Scenario: Cleanup failure
- **WHEN** `CleanupIncomplete("abc-123")` is called
- **AND** deletion fails (e.g., file locked)
- **THEN** a warning SHALL be logged
- **AND** no exception SHALL be thrown

### Requirement: EnsureDirectories creates base and category directories
The `EnsureDirectories` method SHALL create `CompletePath`, `IncompletePath`, and a subdirectory under `CompletePath` for each configured category.

#### Scenario: Directories with categories
- **WHEN** `EnsureDirectories()` is called
- **AND** categories `[{ Name: "tv", Dir: "tv" }, { Name: "movies", Dir: "movies" }]` are configured
- **THEN** `CompletePath`, `IncompletePath`, `CompletePath/tv`, and `CompletePath/movies` SHALL all exist

#### Scenario: Directories without categories
- **WHEN** `EnsureDirectories()` is called
- **AND** no categories are configured
- **THEN** only `CompletePath` and `IncompletePath` SHALL be created

#### Scenario: Category with custom Dir
- **WHEN** `EnsureDirectories()` is called
- **AND** a category has `Name = "dokus"` and `Dir = "dokumentationen"`
- **THEN** a directory `CompletePath/dokumentationen` SHALL be created
