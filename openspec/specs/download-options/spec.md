# Download Options

## Purpose

Configuration model for download paths, concurrency limits, and category-based directory routing. Bound to the `FunkArr:Download` config section.
## Requirements
### Requirement: DownloadOptions config class
The system SHALL define a `DownloadOptions` class in `FunkArr.Core` bound to the `FunkArr:Download` config section containing `Path` (string, default `"data/downloads"`), `ConcurrentDownloads` (int, default 3), `Categories` (list of `DownloadCategory`), `DownloadSchedule` (list of `DownloadTimeSlot`, default empty), `RetryEnabled` (bool, default false), `MaxRetries` (int, default 3), `RetryBackoffBase` (TimeSpan, default 30 seconds), and `MaxHistoryRecords` (int, default 1000).

#### Scenario: Default values
- **WHEN** no `FunkArr:Download` config is provided
- **THEN** `Path` SHALL be `"data/downloads"`, `ConcurrentDownloads` SHALL be `3`, `Categories` SHALL be an empty list, `DownloadSchedule` SHALL be an empty list, `RetryEnabled` SHALL be `false`, `MaxRetries` SHALL be `3`, `RetryBackoffBase` SHALL be `00:00:30`, `MaxHistoryRecords` SHALL be `1000`

#### Scenario: ENV override
- **WHEN** `FunkArr__Download__Path=/shared/downloads` is set
- **THEN** `Path` SHALL be `"/shared/downloads"`

#### Scenario: ENV override for retry
- **WHEN** `FunkArr__Download__RetryEnabled=true` is set
- **THEN** `RetryEnabled` SHALL be `true`

#### Scenario: ENV override for max retries
- **WHEN** `FunkArr__Download__MaxRetries=5` is set
- **THEN** `MaxRetries` SHALL be `5`

#### Scenario: ENV override for backoff base
- **WHEN** `FunkArr__Download__RetryBackoffBase=00:01:00` is set
- **THEN** `RetryBackoffBase` SHALL be 60 seconds

#### Scenario: ENV override for max history records
- **WHEN** `FunkArr__Download__MaxHistoryRecords=500` is set
- **THEN** `MaxHistoryRecords` SHALL be `500`

#### Scenario: Categories via ENV
- **WHEN** `FunkArr__Download__Categories__0__Name=sonarr` and `FunkArr__Download__Categories__1__Name=radarr` are set
- **THEN** `Categories` SHALL contain two entries with names `"sonarr"` and `"radarr"`

#### Scenario: Schedule via ENV
- **WHEN** `FunkArr__Download__DownloadSchedule__0__Start=23:00` and `FunkArr__Download__DownloadSchedule__0__End=02:00` are set
- **THEN** `DownloadSchedule` SHALL contain one entry with Start 23:00 and End 02:00

### Requirement: DownloadCategory model
The system SHALL define a `DownloadCategory` class with `Name` (string, required) and `Dir` (string, optional, defaults to empty string). When `Dir` is empty, the category directory SHALL be the `Name`.

#### Scenario: Category with default Dir
- **WHEN** a category has `Name = "sonarr"` and `Dir = ""`
- **THEN** the resolved directory name SHALL be `"sonarr"`

#### Scenario: Category with custom Dir
- **WHEN** a category has `Name = "dokus"` and `Dir = "dokumentationen"`
- **THEN** the resolved directory name SHALL be `"dokumentationen"`

### Requirement: DownloadTimeSlot model
The system SHALL define a `DownloadTimeSlot` class with `Start` (TimeOnly, required) and `End` (TimeOnly, required).

#### Scenario: Time slot construction
- **WHEN** a `DownloadTimeSlot` is created with `Start = 23:00` and `End = 02:00`
- **THEN** it SHALL represent a download window from 23:00 to 02:00 (over midnight)

### Requirement: DownloadOptions validation
The system SHALL validate `DownloadOptions` on startup using `IValidateOptions<DownloadOptions>`. Validation SHALL reject `DownloadTimeSlot` entries where `Start == End`.

#### Scenario: Valid configuration
- **WHEN** `DownloadSchedule` contains slots with `Start != End`
- **THEN** validation SHALL pass

#### Scenario: Zero-length time slot
- **WHEN** a `DownloadTimeSlot` has `Start == End`
- **THEN** validation SHALL fail with message indicating which slot is invalid

