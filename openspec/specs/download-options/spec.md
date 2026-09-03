# Download Options

## Purpose

Configuration model for download paths, concurrency limits, and category-based directory routing. Bound to the `FunkArr:Download` config section.

## Requirements

### Requirement: DownloadOptions config class
The system SHALL define a `DownloadOptions` class in `FunkArr.Core` bound to the `FunkArr:Download` config section containing `DownloadPath` (string, default `"data/downloads"`), `ConcurrentDownloads` (int, default 3), and `Categories` (list of `DownloadCategory`).

#### Scenario: Default values
- **WHEN** no `FunkArr:Download` config is provided
- **THEN** `DownloadPath` SHALL be `"data/downloads"`, `ConcurrentDownloads` SHALL be `3`, and `Categories` SHALL be an empty list

#### Scenario: ENV override
- **WHEN** `FunkArr__Download__DownloadPath=/media/downloads` is set
- **THEN** `DownloadPath` SHALL be `"/media/downloads"`

#### Scenario: Categories via ENV
- **WHEN** `FunkArr__Download__Categories__0__Name=sonarr` and `FunkArr__Download__Categories__1__Name=radarr` are set
- **THEN** `Categories` SHALL contain two entries with names `"sonarr"` and `"radarr"`

### Requirement: DownloadCategory model
The system SHALL define a `DownloadCategory` class with `Name` (string, required) and `Dir` (string, optional, defaults to empty string). When `Dir` is empty, the category directory SHALL be the `Name`.

#### Scenario: Category with default Dir
- **WHEN** a category has `Name = "sonarr"` and `Dir = ""`
- **THEN** the resolved directory name SHALL be `"sonarr"`

#### Scenario: Category with custom Dir
- **WHEN** a category has `Name = "dokus"` and `Dir = "dokumentationen"`
- **THEN** the resolved directory name SHALL be `"dokumentationen"`

### Requirement: CompletePath derived property
The system SHALL expose a `CompletePath` property on `DownloadOptions` computed as `Path.Combine(DownloadPath, "complete")`.

#### Scenario: CompletePath derivation
- **WHEN** `DownloadPath` is `"/downloads"`
- **THEN** `CompletePath` SHALL be `"/downloads/complete"`

### Requirement: IncompletePath derived property
The system SHALL expose an `IncompletePath` property on `DownloadOptions` computed as `Path.Combine(DownloadPath, "incomplete")`.

#### Scenario: IncompletePath derivation
- **WHEN** `DownloadPath` is `"/downloads"`
- **THEN** `IncompletePath` SHALL be `"/downloads/incomplete"`

### Requirement: ResolveCategoryDir method
The system SHALL expose a `ResolveCategoryDir(string category)` method on `DownloadOptions` that returns the resolved directory name for a category, or empty string if the category is empty or not found.

#### Scenario: Known category
- **WHEN** `ResolveCategoryDir("sonarr")` is called and a category with `Name = "sonarr"` exists
- **THEN** the result SHALL be `"sonarr"` (or the custom `Dir` if set)

#### Scenario: Unknown category
- **WHEN** `ResolveCategoryDir("unknown")` is called and no matching category exists
- **THEN** the result SHALL be `""`

#### Scenario: Empty category
- **WHEN** `ResolveCategoryDir("")` is called
- **THEN** the result SHALL be `""`

#### Scenario: Case-insensitive matching
- **WHEN** `ResolveCategoryDir("Sonarr")` is called and a category with `Name = "sonarr"` exists
- **THEN** the result SHALL match and return the resolved directory
