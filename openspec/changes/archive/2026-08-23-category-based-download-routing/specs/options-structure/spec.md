## MODIFIED Requirements

### Requirement: Domain-scoped options classes
The system SHALL expose configuration through domain-scoped `IOptions<T>` classes instead of a single monolithic options class. The following classes SHALL exist in `FunkArr.Configuration`:

- `FunkArrOptions` — cross-cutting and bootstrap settings: `ApiKey`, `PersistencePath`, `Postgres`, `MatchLedgerCapacity`.
- `DownloadOptions` — download pipeline settings: `Path`, `TempPath`, `ConcurrentDownloads`, `PathMapping`, `Category`.
- `RuleSetOptions` — rule-set source and refresh settings: `Repository`, `Version`, `Path`, `RefreshIntervalMinutes`.
- `QualityOptions` — quality-probe cache settings: `Probing`, `CacheTtlMinutes`, `CacheCapacity`.
- `SearchOptions` — search-time settings: `QualityProbeLimit`, `TmdbApiKey`.

#### Scenario: Consumer injects only the options it needs
- **WHEN** a class depends only on download-pipeline settings (e.g. `DownloadQueueActor`)
- **THEN** it SHALL inject `IOptions<DownloadOptions>` and SHALL NOT inject `IOptions<FunkArrOptions>` or any other domain options class

#### Scenario: Consumer needing multiple domains injects each explicitly
- **WHEN** a class depends on settings from more than one domain (e.g. `SetupController` needing download paths and the API key)
- **THEN** it SHALL inject one `IOptions<T>` per domain it actually reads, with no single options type carrying settings the consumer doesn't use

### Requirement: Preserved default values
Each property on each domain options class SHALL retain the same default value it had as a property on the original monolithic `FunkArrOptions`, when no configuration is supplied.

#### Scenario: Defaults match pre-decomposition values
- **WHEN** no `FunkArr:Download`, `FunkArr:RuleSet`, `FunkArr:Quality`, or `FunkArr:Search` configuration is supplied
- **THEN** `DownloadOptions.Path` SHALL default to `/media/downloads`, `DownloadOptions.TempPath` SHALL default to `data/temp`, `DownloadOptions.ConcurrentDownloads` SHALL default to `3`
- **AND** `RuleSetOptions.Repository` SHALL default to `st0o0/funkarr`, `RuleSetOptions.Version` SHALL default to `latest`, `RuleSetOptions.Path` SHALL default to `data/rulesets`, `RuleSetOptions.RefreshIntervalMinutes` SHALL default to `60`
- **AND** `QualityOptions.Probing` SHALL default to `true`, `QualityOptions.CacheTtlMinutes` SHALL default to `360`, `QualityOptions.CacheCapacity` SHALL default to `50000`
- **AND** `SearchOptions.QualityProbeLimit` SHALL default to `30`

## ADDED Requirements

### Requirement: Category dictionary on DownloadOptions
`DownloadOptions` SHALL have a `Dictionary<string, string> Category` property that maps category names to output directory paths. The dictionary SHALL default to an empty dictionary.

#### Scenario: Category configured via environment variables
- **WHEN** environment variables `FunkArr__Download__Category__tv=tv` and `FunkArr__Download__Category__movies=/data/movies` are set
- **THEN** `DownloadOptions.Category` SHALL contain `{"tv": "tv", "movies": "/data/movies"}`

#### Scenario: No categories configured
- **WHEN** no `FunkArr__Download__Category__*` environment variables are set
- **THEN** `DownloadOptions.Category` SHALL be an empty dictionary

## RENAMED Requirements

### Requirement: Preserved default values
- **FROM:** `DownloadOptions.DownloadPath`
- **TO:** `DownloadOptions.Path`
