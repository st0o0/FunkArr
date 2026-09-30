## ADDED Requirements

### Requirement: DownloadPaths value object
The system SHALL define a `DownloadPaths` sealed record in `FunkArr.Download` with three properties: `IncompletePath` (string, full absolute path to the temp MKV file), `CompletePath` (string, full absolute path to the final MKV file), and `RelativePath` (string, path relative to `DownloadOptions.CompletePath`).

#### Scenario: Record structure
- **WHEN** a `DownloadPaths` instance is inspected
- **THEN** it SHALL have `IncompletePath`, `CompletePath`, and `RelativePath` properties
- **AND** `CompletePath` SHALL equal `Path.Combine(options.CompletePath, RelativePath)`

### Requirement: DownloadPaths.Compute factory method
The system SHALL define a static `Compute(string entityId, string title, string? category, DownloadOptions options)` method on `DownloadPaths` as the single entry point for all path computation in the download pipeline. Path construction SHALL use `Path.Join` (not `Path.Combine`) to prevent silent segment discard when a segment is accidentally rooted. The final result SHALL be normalized via `Path.GetFullPath`.

#### Scenario: Compute with episode identifier and category
- **WHEN** `DownloadPaths.Compute("abc-123", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", options)` is called
- **AND** category `"tv"` resolves to directory `"tv"`
- **THEN** `IncompletePath` SHALL be `"{IncompletePath}/abc-123/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"`
- **AND** `CompletePath` SHALL be `"{CompletePath}/tv/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"`
- **AND** `RelativePath` SHALL be `"tv/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr/Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"`

#### Scenario: Compute with date identifier
- **WHEN** `DownloadPaths.Compute("abc-123", "Show.2026-09-03.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", options)` is called
- **THEN** the directory name SHALL NOT include a disambiguator (date counts as episode identifier)

#### Scenario: Compute without episode identifier
- **WHEN** `DownloadPaths.Compute("a1b2c3d4-e5f6-7890-abcd-ef1234567890", "Show.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", options)` is called
- **AND** the title contains no episode or date pattern
- **THEN** the directory name SHALL be `"Show.Title.GERMAN.720p.WEB.h264-FunkArr-a1b2c3d4"` (first 8 chars of entityId as disambiguator)
- **AND** the filename SHALL remain `"Show.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"` (no disambiguator)

#### Scenario: Compute with unknown category
- **WHEN** `DownloadPaths.Compute("abc-123", "Show.S01E05", "unknown", options)` is called
- **AND** no matching category exists in `options.Categories`
- **THEN** the category subdirectory SHALL be omitted from all paths

#### Scenario: Compute with null category
- **WHEN** `DownloadPaths.Compute("abc-123", "Show.S01E05", null, options)` is called
- **THEN** the category subdirectory SHALL be omitted from all paths

#### Scenario: Compute with custom category Dir
- **WHEN** `DownloadPaths.Compute("abc-123", "Show.S01E05", "dokus", options)` is called
- **AND** the category has `Name = "dokus"` and `Dir = "dokumentationen"`
- **THEN** the category subdirectory SHALL be `"dokumentationen"` (not `"dokus"`)

### Requirement: Category resolution logic
The `DownloadPaths.Compute` method SHALL resolve a category name to a directory name using `DownloadOptions.Categories`. When `Dir` is empty, the category `Name` SHALL be used. Matching SHALL be case-insensitive.

#### Scenario: Category with default Dir
- **WHEN** a category has `Name = "sonarr"` and `Dir = ""`
- **THEN** the resolved directory SHALL be `"sonarr"`

#### Scenario: Category with custom Dir
- **WHEN** a category has `Name = "dokus"` and `Dir = "dokumentationen"`
- **THEN** the resolved directory SHALL be `"dokumentationen"`

#### Scenario: Case-insensitive matching
- **WHEN** category `"Sonarr"` is resolved and a category with `Name = "sonarr"` exists
- **THEN** it SHALL match and use the resolved directory

### Requirement: Episode identifier detection
The `DownloadPaths.Compute` method SHALL detect episode identifiers in a title using regex patterns: `S\d{2,}E\d{2,}` (season+episode), `.E\d{2,}.` (episode only with dot delimiters), or `\d{4}-\d{2}-\d{2}` (date). Detection SHALL be case-insensitive.

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
