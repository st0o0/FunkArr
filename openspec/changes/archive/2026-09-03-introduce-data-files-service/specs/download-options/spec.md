## MODIFIED Requirements

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

## REMOVED Requirements

### Requirement: CompletePath derived property
**Reason**: Path derivation moved to `DataPaths`. `DataPaths.Complete` provides the same value computed from `DownloadOptions.Path`.
**Migration**: Replace `downloadOptions.CompletePath` with `dataPaths.Complete`.

### Requirement: IncompletePath derived property
**Reason**: Path derivation moved to `DataPaths`. `DataPaths.Incomplete` provides the same value computed from `DownloadOptions.Path`.
**Migration**: Replace `downloadOptions.IncompletePath` with `dataPaths.Incomplete`.
