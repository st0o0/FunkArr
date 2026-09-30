## MODIFIED Requirements

### Requirement: DownloadOptions config class
The system SHALL define a `DownloadOptions` class in `FunkArr.Core` bound to the `FunkArr:Download` config section containing `Path` (string, default `"data/downloads"`), `ConcurrentDownloads` (int, default 3), `Categories` (list of `DownloadCategory`), and `DownloadSchedule` (list of `DownloadTimeSlot`, default empty).

#### Scenario: Default values
- **WHEN** no `FunkArr:Download` config is provided
- **THEN** `Path` SHALL be `"data/downloads"`, `ConcurrentDownloads` SHALL be `3`, `Categories` SHALL be an empty list, and `DownloadSchedule` SHALL be an empty list

#### Scenario: ENV override
- **WHEN** `FunkArr__Download__Path=/shared/downloads` is set
- **THEN** `Path` SHALL be `"/shared/downloads"`

#### Scenario: Categories via ENV
- **WHEN** `FunkArr__Download__Categories__0__Name=sonarr` and `FunkArr__Download__Categories__1__Name=radarr` are set
- **THEN** `Categories` SHALL contain two entries with names `"sonarr"` and `"radarr"`

#### Scenario: Schedule via ENV
- **WHEN** `FunkArr__Download__DownloadSchedule__0__Start=23:00` and `FunkArr__Download__DownloadSchedule__0__End=02:00` are set
- **THEN** `DownloadSchedule` SHALL contain one entry with Start 23:00 and End 02:00

### Requirement: DownloadOptions validation
The system SHALL validate `DownloadOptions` on startup using `IValidateOptions<DownloadOptions>`. Validation SHALL reject `DownloadTimeSlot` entries where `Start == End`.

#### Scenario: Valid configuration
- **WHEN** `DownloadSchedule` contains slots with `Start != End`
- **THEN** validation SHALL pass

#### Scenario: Zero-length time slot
- **WHEN** a `DownloadTimeSlot` has `Start == End`
- **THEN** validation SHALL fail with message indicating which slot is invalid

## REMOVED Requirements

### Requirement: SpeedLimitBytesPerSecond in DownloadOptions
**Reason**: The speed limit feature used FFmpeg's `-maxrate`/`-bufsize` flags which are encoder parameters — ignored during codec-copy remux. The feature was non-functional.
**Migration**: Remove `SpeedLimitBytesPerSecond` from configuration. No behavioral change since the setting had no effect.

### Requirement: SpeedLimitBytesPerSecond validation
**Reason**: Removed together with the SpeedLimitBytesPerSecond property.
**Migration**: Remove negative speed limit validation check from `DownloadOptionsValidator`.
