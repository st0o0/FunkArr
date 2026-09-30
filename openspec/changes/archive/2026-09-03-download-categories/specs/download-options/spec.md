## MODIFIED Requirements

### Requirement: DownloadOptions config class
The system SHALL define a `DownloadOptions` class in `FunkArr.Core` bound to the `FunkArr:Download` config section containing `DownloadPath` (string, default `"data/downloads"`), `ConcurrentDownloads` (int, default 3), and `Categories` (list of `DownloadCategory`). Default `Categories` SHALL include `tv` and `movies`.

#### Scenario: Default values
- **WHEN** no `FunkArr:Download` config is provided
- **THEN** `DownloadPath` SHALL be `"data/downloads"`, `ConcurrentDownloads` SHALL be `3`, and `Categories` SHALL contain two entries: `{ Name: "tv", Dir: "tv" }` and `{ Name: "movies", Dir: "movies" }`

#### Scenario: ENV override
- **WHEN** `FunkArr__Download__DownloadPath=/media/downloads` is set
- **THEN** `DownloadPath` SHALL be `"/media/downloads"`

#### Scenario: Categories via ENV
- **WHEN** `FunkArr__Download__Categories__0__Name=sonarr` and `FunkArr__Download__Categories__1__Name=radarr` are set
- **THEN** `Categories` SHALL contain two entries with names `"sonarr"` and `"radarr"`
