# Capability: Category Routing

## Purpose

Resolves download category strings to output directory paths using configurable mappings, enabling Sonarr/Radarr category-based file organization (e.g., tv vs movies going to different directories).

## Requirements

### Requirement: Category path resolution
The system SHALL provide a static `CategoryResolver.Resolve(basePath, category, categoryConfig)` method that resolves a category string to an output directory path using three-tier logic:
1. If no category is provided (null or empty), return `basePath` directly.
2. If a matching entry exists in `categoryConfig` and the value is a rooted path (`Path.IsPathRooted`), use it as-is.
3. If a matching entry exists and the value is a relative path, combine it with `basePath`.
4. If no entry exists, sanitize the category name and use it as a subfolder under `basePath`.

`FileService` SHALL call `CategoryResolver.Resolve` internally from `GetOutputPath(title, category?)` and `EnsureOutputDirectory(title, category?)`. No caller outside FileService needs to invoke CategoryResolver directly.

#### Scenario: Absolute path override
- **WHEN** category is `"movies"` and `DownloadOptions.Category` contains `{"movies": "/data/movies/incoming"}`
- **THEN** the resolved output directory SHALL be `/data/movies/incoming`

#### Scenario: Relative path override
- **WHEN** category is `"tv"` and `DownloadOptions.Category` contains `{"tv": "serien"}` and `DownloadOptions.Path` is `/downloads/complete`
- **THEN** the resolved output directory SHALL be `/downloads/complete/serien`

#### Scenario: Subfolder fallback for unknown category
- **WHEN** category is `"anime"` and `DownloadOptions.Category` has no entry for `"anime"` and `DownloadOptions.Path` is `/downloads/complete`
- **THEN** the resolved output directory SHALL be `/downloads/complete/anime`

#### Scenario: No category provided
- **WHEN** category is null or empty and `DownloadOptions.Path` is `/downloads/complete`
- **THEN** the resolved output directory SHALL be `/downloads/complete`

### Requirement: Category resolution is case-sensitive
The system SHALL resolve category names case-sensitively against `DownloadOptions.Category` entries. The `Dictionary<string, string>` uses the default (ordinal) comparer, so `"TV"` and `"tv"` are distinct keys.

#### Scenario: Case-sensitive lookup matches exact key
- **WHEN** category is `"tv"` and `DownloadOptions.Category` contains `{"tv": "serien"}`
- **THEN** the entry SHALL match and resolve to `Path/serien`

#### Scenario: Case mismatch falls through to subfolder
- **WHEN** category is `"TV"` and `DownloadOptions.Category` contains `{"tv": "serien"}` (lowercase key only)
- **THEN** the entry SHALL NOT match and the system SHALL fall back to using `"TV"` as a sanitized subfolder name

### Requirement: Category name filesystem safety
When using the category name as a fallback subfolder, the system SHALL sanitize the name by removing characters invalid for directory names.

#### Scenario: Invalid characters removed
- **WHEN** category is `"tv/shows"` and no configured override exists
- **THEN** the resolved subfolder name SHALL have the invalid path separator removed
