## MODIFIED Requirements

### Requirement: IDownloadFileService interface
The system SHALL define an `IDownloadFileService` interface in `FunkArr.Download` with methods for path resolution, directory management, file moves, and cleanup.

#### Scenario: Interface definition
- **WHEN** the `IDownloadFileService` interface is inspected
- **THEN** it SHALL declare `EnsureIncompletePath(string entityId, string title)` returning `string`
- **AND** `ResolveOutputPath(string entityId, string title, string? category)` returning `string`
- **AND** `MoveToComplete(string tempPath, string outputPath)` returning `void`
- **AND** `CleanupIncomplete(string entityId)` returning `void`
- **AND** `EnsureDirectories()` returning `void`

### Requirement: DownloadFileService implementation
The system SHALL provide a `DownloadFileService` sealed class in `FunkArr.Download` implementing `IDownloadFileService` and `IHostedLifecycleService`, injecting `IOptions<DownloadOptions>`.

#### Scenario: DI registration
- **WHEN** the service collection is configured
- **THEN** `IDownloadFileService` SHALL be registered as a singleton mapping to `DownloadFileService`
- **AND** the same instance SHALL be registered as `IHostedLifecycleService`

#### Scenario: Startup bootstrapping
- **WHEN** the application starts
- **THEN** `DownloadFileService.StartingAsync` SHALL call `EnsureDirectories()` to create base and category directories

### Requirement: EnsureIncompletePath creates directory and returns temp file path
The `EnsureIncompletePath` method SHALL create the per-entity incomplete directory and return the full path to the temporary MKV file.

#### Scenario: Incomplete path resolution and creation
- **WHEN** `EnsureIncompletePath("abc-123", "My.Show.S01E01")` is called
- **AND** `IncompletePath` is `"/downloads/incomplete"`
- **THEN** the directory `"/downloads/incomplete/abc-123"` SHALL be created if it does not exist
- **AND** the result SHALL be the full path of `"/downloads/incomplete/abc-123/My.Show.S01E01.mkv"`

### Requirement: ResolveOutputPath computes category-routed complete path with collision safety
The `ResolveOutputPath` method SHALL return the full path to the final MKV file under the complete directory, routed into a category subdirectory when the category resolves. When the title lacks an episode identifier, the entity ID SHALL be used to disambiguate the output directory name.

#### Scenario: Output path with episode identifier
- **WHEN** `ResolveOutputPath("abc-123", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr", "tv")` is called
- **AND** category `"tv"` resolves to directory `"tv"`
- **THEN** the result SHALL be the full path of `"/downloads/complete/tv/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"`
- **AND** the directory name SHALL NOT include a disambiguator

#### Scenario: Output path with date identifier
- **WHEN** `ResolveOutputPath("abc-123", "Show.2026-09-03.Title.GERMAN.720p.WEB.h264-FunkArr", "tv")` is called
- **THEN** the directory name SHALL NOT include a disambiguator

#### Scenario: Output path without any episode identifier
- **WHEN** `ResolveOutputPath("a1b2c3d4-e5f6-7890-abcd-ef1234567890", "Show.Title.GERMAN.720p.WEB.h264-FunkArr", "tv")` is called
- **AND** the title contains no `S\d{2,}E\d{2,}`, `E\d{2,}`, or `\d{4}-\d{2}-\d{2}` pattern
- **THEN** the output directory name SHALL be `"Show.Title.GERMAN.720p.WEB.h264-FunkArr-a1b2c3d4"`
- **AND** the filename SHALL remain `"Show.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"` (no disambiguator)

#### Scenario: Output path with unknown category and no identifier
- **WHEN** `ResolveOutputPath("a1b2c3d4-...", "Show.Title.GERMAN.720p.WEB.h264-FunkArr", "unknown")` is called
- **AND** no matching category exists
- **THEN** the result SHALL omit the category subdirectory but still include the disambiguator in the directory name

### Requirement: Episode identifier detection
The system SHALL detect episode identifiers in a title using regex patterns: `S\d{2,}E\d{2,}` (season+episode), `E\d{2,}` (episode only), or `\d{4}-\d{2}-\d{2}` (date). Detection SHALL be case-insensitive.

#### Scenario: Season and episode detected
- **WHEN** `"Show.S01E05.Title"` is checked
- **THEN** it SHALL be identified as having an episode identifier

#### Scenario: Episode only detected
- **WHEN** `"Show.E05.Title"` is checked
- **THEN** it SHALL be identified as having an episode identifier

#### Scenario: Date detected
- **WHEN** `"Show.2026-09-03.Title"` is checked
- **THEN** it SHALL be identified as having an episode identifier

#### Scenario: No identifier
- **WHEN** `"Show.Title.GERMAN.720p"` is checked
- **THEN** it SHALL be identified as lacking an episode identifier

## RENAMED Requirements

### Requirement: ResolveTempPath computes incomplete working directory
- **FROM:** ResolveTempPath computes incomplete working directory
- **TO:** EnsureIncompletePath creates directory and returns temp file path
