## MODIFIED Requirements

### Requirement: Domain-scoped options classes
The system SHALL expose configuration through domain-scoped `IOptions<T>` classes instead of a single monolithic options class. The following classes SHALL exist in `FunkArr.Configuration`:

- `FunkArrOptions` — cross-cutting and bootstrap settings: `ApiKey`, `PersistencePath`, `Postgres`, `MatchLedgerCapacity`.
- `DownloadOptions` — download pipeline settings: `DownloadPath`, `TempPath`, `ConcurrentDownloads`, `PathMapping`.
- `RuleSetOptions` — rule-set source and refresh settings: `Repository`, `Version`, `Path`, `RefreshIntervalMinutes`.
- `QualityOptions` — quality-probe cache settings: `Probing`, `CacheTtlMinutes`, `CacheCapacity`.
- `SearchOptions` — search-time settings: `QualityProbeLimit`, `TmdbApiKey`.

`Prowlarr` and `ArrInstances` are removed from `FunkArrOptions`. FunkArr does not store external app credentials.

#### Scenario: Consumer injects only the options it needs
- **WHEN** a class depends only on download-pipeline settings (e.g. `DownloadQueueActor`)
- **THEN** it SHALL inject `IOptions<DownloadOptions>` and SHALL NOT inject `IOptions<FunkArrOptions>` or any other domain options class

#### Scenario: Consumer needing multiple domains injects each explicitly
- **WHEN** a class depends on settings from more than one domain (e.g. `SetupController` needing download paths and the API key)
- **THEN** it SHALL inject one `IOptions<T>` per domain it actually reads, with no single options type carrying settings the consumer doesn't use

### Requirement: Default API key
`FunkArrOptions.ApiKey` SHALL default to `"funkarr-default-api-key"` in `appsettings.json`. The key exists only because Sonarr/Radarr/Prowlarr require a non-empty API key field when adding indexers and download clients. Users MAY override via the `FunkArr__ApiKey` environment variable.

#### Scenario: Default key works out of the box
- **WHEN** no `FunkArr__ApiKey` environment variable is set
- **THEN** `FunkArrOptions.ApiKey` SHALL be `"funkarr-default-api-key"`

#### Scenario: Custom key via environment variable
- **WHEN** `FunkArr__ApiKey` is set to `"my-custom-key"`
- **THEN** `FunkArrOptions.ApiKey` SHALL be `"my-custom-key"`

## REMOVED Requirements

### Requirement: Documented breaking change for flat environment variables
**Reason**: This requirement documented the migration from flat to nested env vars introduced in the options-structure change. It is a historical migration note, not an ongoing requirement. Removing it from the spec as it no longer applies to new deployments.
**Migration**: None — the nested env var format is already the standard.
