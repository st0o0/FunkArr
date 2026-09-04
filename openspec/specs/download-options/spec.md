# Download Options

## Purpose

Configuration model for download paths, concurrency limits, and category-based directory routing. Bound to the `FunkArr:Download` config section.

## Requirements

### Requirement: DownloadOptions config class
The system SHALL define a `DownloadOptions` class in `FunkArr.Core` bound to the `FunkArr:Download` config section containing `Path` (string, default `"data/downloads"`), `ConcurrentDownloads` (int, default 3), and `Categories` (list of `DownloadCategory`).

#### Scenario: Default values
- **WHEN** no `FunkArr:Download` config is provided
- **THEN** `Path` SHALL be `"data/downloads"`, `ConcurrentDownloads` SHALL be `3`, and `Categories` SHALL be an empty list

#### Scenario: ENV override
- **WHEN** `FunkArr__Download__Path=/shared/downloads` is set
- **THEN** `Path` SHALL be `"/shared/downloads"`

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


