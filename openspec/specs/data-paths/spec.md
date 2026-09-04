# Data Paths

## Purpose

Convention-based path resolution for all data locations, computed once at startup from two configurable roots (`DataPath` and `Download:Path`).

## Requirements

### Requirement: DataPaths class
The system SHALL define a `DataPaths` sealed class in `FunkArr.Core` that computes all data directory paths from two configurable roots. It SHALL be registered as a singleton and computed once at startup from `FunkArrOptions` and `DownloadOptions`.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `DataPaths` SHALL be registered as a singleton, constructed from the current `FunkArrOptions` and `DownloadOptions` values

### Requirement: DataPaths resolves data root paths
`DataPaths` SHALL expose absolute, resolved paths for all data locations under `FunkArrOptions.DataPath`. All paths SHALL be computed via `Path.GetFullPath` at construction time.

#### Scenario: Default data paths
- **WHEN** `DataPath` is `"data"`
- **THEN** `DataRoot` SHALL be the absolute path of `"data"`
- **AND** `Database` SHALL be `"{DataRoot}/funkarr.db"`
- **AND** `CommunityRuleSets` SHALL be `"{DataRoot}/rulesets/community"`
- **AND** `LocalRuleSets` SHALL be `"{DataRoot}/rulesets/local"`
- **AND** `RuleSetVersion` SHALL be `"{DataRoot}/rulesets/version.txt"`
- **AND** `Temp` SHALL be `"{DataRoot}/temp"`

#### Scenario: Custom data path
- **WHEN** `DataPath` is `"/app/data"`
- **THEN** `DataRoot` SHALL be `"/app/data"`
- **AND** `Database` SHALL be `"/app/data/funkarr.db"`
- **AND** `CommunityRuleSets` SHALL be `"/app/data/rulesets/community"`

### Requirement: DataPaths resolves download root paths
`DataPaths` SHALL expose absolute, resolved paths for download locations under `DownloadOptions.Path`. All paths SHALL be computed via `Path.GetFullPath` at construction time.

#### Scenario: Default download paths
- **WHEN** `DownloadOptions.Path` is `"data/downloads"`
- **THEN** `DownloadRoot` SHALL be the absolute path of `"data/downloads"`
- **AND** `Incomplete` SHALL be `"{DownloadRoot}/incomplete"`
- **AND** `Complete` SHALL be `"{DownloadRoot}/complete"`

#### Scenario: Custom download path via ENV
- **WHEN** `FunkArr__Download__Path` is set to `"/shared/downloads"`
- **THEN** `DownloadRoot` SHALL be `"/shared/downloads"`
- **AND** `Incomplete` SHALL be `"/shared/downloads/incomplete"`
- **AND** `Complete` SHALL be `"/shared/downloads/complete"`

### Requirement: DataPaths resolves download file paths
`DataPaths` SHALL provide a `ResolveDownload` method that computes incomplete, complete, and relative paths for a specific download. Path construction SHALL use `Path.Join` (not `Path.Combine`) to prevent silent segment discard.

#### Scenario: Resolve with episode identifier and category
- **WHEN** `ResolveDownload("abc-123", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", categories)` is called
- **AND** category `"tv"` resolves to directory `"tv"`
- **THEN** `IncompletePath` SHALL be `"{Incomplete}/abc-123/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"`
- **AND** `CompletePath` SHALL be `"{Complete}/tv/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"`
- **AND** `RelativePath` SHALL be `"tv/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"`

#### Scenario: Resolve without episode identifier
- **WHEN** `ResolveDownload("a1b2c3d4-e5f6-7890-abcd-ef1234567890", "Show.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", categories)` is called
- **AND** the title contains no episode or date pattern
- **THEN** the directory name SHALL be `"Show.Title.GERMAN.720p.WEB.h264-FunkArr-a1b2c3d4"` (first 8 chars of entityId as disambiguator)
- **AND** the filename SHALL remain `"Show.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"` (no disambiguator)

#### Scenario: Resolve with date identifier
- **WHEN** `ResolveDownload("abc-123", "Show.2026-09-03.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", categories)` is called
- **THEN** the directory name SHALL NOT include a disambiguator (date counts as episode identifier)

#### Scenario: Resolve with unknown category
- **WHEN** `ResolveDownload("abc-123", "Show.S01E05", "unknown", categories)` is called
- **AND** no matching category exists
- **THEN** the category subdirectory SHALL be omitted from all paths

#### Scenario: Resolve with null category
- **WHEN** `ResolveDownload("abc-123", "Show.S01E05", null, categories)` is called
- **THEN** the category subdirectory SHALL be omitted from all paths

#### Scenario: Resolve with custom category Dir
- **WHEN** `ResolveDownload("abc-123", "Show.S01E05", "dokus", categories)` is called
- **AND** the category has `Name = "dokus"` and `Dir = "dokumentationen"`
- **THEN** the category subdirectory SHALL be `"dokumentationen"` (not `"dokus"`)

### Requirement: Category resolution logic
The `ResolveDownload` method SHALL resolve a category name to a directory name using the provided categories list. When `Dir` is empty, the category `Name` SHALL be used. Matching SHALL be case-insensitive.

#### Scenario: Category with default Dir
- **WHEN** a category has `Name = "sonarr"` and `Dir = ""`
- **THEN** the resolved directory SHALL be `"sonarr"`

#### Scenario: Case-insensitive matching
- **WHEN** category `"Sonarr"` is resolved and a category with `Name = "sonarr"` exists
- **THEN** it SHALL match and use the resolved directory

### Requirement: Episode identifier detection
The `ResolveDownload` method SHALL detect episode identifiers in a title using regex patterns: `S\d{2,}E\d{2,}` (season+episode), `.E\d{2,}.` (episode only with dot delimiters), or `\d{4}-\d{2}-\d{2}` (date). Detection SHALL be case-insensitive.

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

### Requirement: ResolvedDownload value object
The `ResolveDownload` method SHALL return a `ResolvedDownload` sealed record with `IncompletePath`, `CompletePath`, and `RelativePath` string properties.

#### Scenario: Record structure
- **WHEN** a `ResolvedDownload` instance is inspected
- **THEN** it SHALL have `IncompletePath`, `CompletePath`, and `RelativePath` properties
