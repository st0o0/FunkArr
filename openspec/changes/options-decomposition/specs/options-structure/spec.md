# options-structure

## ADDED Requirements

### Requirement: Domain-scoped options classes
The system SHALL expose configuration through domain-scoped `IOptions<T>` classes instead of a single monolithic options class. The following classes SHALL exist in `FunkArr.Configuration`:

- `FunkArrOptions` — cross-cutting and bootstrap settings: `ApiKey`, `LogFormat`, `PersistencePath`, `Postgres`, `MatchLedgerCapacity`, `Prowlarr`, `ArrInstances`.
- `DownloadOptions` — download pipeline settings: `DownloadPath`, `TempPath`, `ConcurrentDownloads`, `PathMapping`.
- `RuleSetOptions` — rule-set source and refresh settings: `SourceUrl`, `Repository`, `Version`, `RefreshMode`, `Path`, `RefreshIntervalMinutes`.
- `QualityOptions` — quality-probe cache settings: `Probing`, `CacheTtlMinutes`, `CacheCapacity`.
- `SearchOptions` — search-time settings: `QualityProbeLimit`.

#### Scenario: Consumer injects only the options it needs
- **WHEN** a class depends only on download-pipeline settings (e.g. `DownloadQueueActor`)
- **THEN** it SHALL inject `IOptions<DownloadOptions>` and SHALL NOT inject `IOptions<FunkArrOptions>` or any other domain options class

#### Scenario: Consumer needing multiple domains injects each explicitly
- **WHEN** a class depends on settings from more than one domain (e.g. `SetupEndpoints` needing download paths, rule-set config, and the API key)
- **THEN** it SHALL inject one `IOptions<T>` per domain it actually reads, with no single options type carrying settings the consumer doesn't use

### Requirement: Nested config section binding
Each domain options class SHALL bind to a nested configuration section under the root `FunkArr` section, keyed by the domain name, using standard `IConfiguration.GetSection(...)` binding.

- `FunkArrOptions` SHALL bind to section `FunkArr`.
- `DownloadOptions` SHALL bind to section `FunkArr:Download`.
- `RuleSetOptions` SHALL bind to section `FunkArr:RuleSet`.
- `QualityOptions` SHALL bind to section `FunkArr:Quality`.
- `SearchOptions` SHALL bind to section `FunkArr:Search`.

#### Scenario: Nested section values bind correctly
- **WHEN** `appsettings.json` contains `FunkArr:Download:ConcurrentDownloads = 5`
- **THEN** `IOptions<DownloadOptions>.Value.ConcurrentDownloads` SHALL equal `5`

#### Scenario: Nested section binds from environment variables
- **WHEN** the environment variable `FunkArr__Download__ConcurrentDownloads` is set to `5`
- **THEN** `IOptions<DownloadOptions>.Value.ConcurrentDownloads` SHALL equal `5`, consistent with ASP.NET Core's standard double-underscore section-path convention

#### Scenario: Sibling sections do not collide
- **WHEN** both `FunkArr:RuleSet:Path` and `FunkArr:Download:PathMapping` are set
- **THEN** binding `RuleSetOptions` SHALL NOT be affected by `DownloadOptions` values and vice versa

### Requirement: Per-section startup validation
Each domain options class SHALL have its own `IValidateOptions<T>` implementation, registered independently, and each options registration SHALL call `.ValidateOnStart()` so invalid configuration in any single section fails application startup with a message scoped to that section.

#### Scenario: Invalid value in one section fails startup with a scoped message
- **WHEN** `FunkArr:Quality:CacheCapacity` is configured below its minimum
- **THEN** the host SHALL fail to start
- **AND** the validation failure message SHALL reference `FunkArr:Quality:CacheCapacity`, not an unrelated section

#### Scenario: Valid configuration in all sections starts successfully
- **WHEN** every domain section satisfies its validator's rules
- **THEN** the host SHALL start without any `OptionsValidationException`

#### Scenario: One section's invalid value does not block validation of others
- **WHEN** `FunkArr:Download:ConcurrentDownloads` is invalid but all other sections are valid
- **THEN** the validation failure SHALL be attributable specifically to `DownloadOptions`, independent of whether `RuleSetOptions`, `QualityOptions`, `SearchOptions`, or `FunkArrOptions` validators ran

### Requirement: Preserved default values
Each property on each domain options class SHALL retain the same default value it had as a property on the original monolithic `FunkArrOptions`, when no configuration is supplied.

#### Scenario: Defaults match pre-decomposition values
- **WHEN** no `FunkArr:Download`, `FunkArr:RuleSet`, `FunkArr:Quality`, or `FunkArr:Search` configuration is supplied
- **THEN** `DownloadOptions.DownloadPath` SHALL default to `/media/downloads`, `DownloadOptions.TempPath` SHALL default to `data/temp`, `DownloadOptions.ConcurrentDownloads` SHALL default to `3`
- **AND** `RuleSetOptions.SourceUrl` SHALL default to `https://raw.githubusercontent.com/rundfunkarr/rundfunkarr/main/data/rulesets.json`, `RuleSetOptions.Repository` SHALL default to `st0o0/funkarr`, `RuleSetOptions.Version` SHALL default to `latest`, `RuleSetOptions.RefreshMode` SHALL default to `github-release`, `RuleSetOptions.Path` SHALL default to `data/rulesets`, `RuleSetOptions.RefreshIntervalMinutes` SHALL default to `60`
- **AND** `QualityOptions.Probing` SHALL default to `true`, `QualityOptions.CacheTtlMinutes` SHALL default to `360`, `QualityOptions.CacheCapacity` SHALL default to `50000`
- **AND** `SearchOptions.QualityProbeLimit` SHALL default to `30`

### Requirement: Documented breaking change for flat environment variables
Because the configuration shape moves from flat (`FunkArr:DownloadPath`) to nested (`FunkArr:Download:DownloadPath`) sections, the system SHALL NOT silently continue to accept the old flat keys. The change SHALL instead be surfaced through updated reference configuration and documentation.

#### Scenario: Old flat env var no longer takes effect
- **WHEN** only the old flat variable `FunkArr__DownloadPath` is set (and `FunkArr__Download__DownloadPath` is not)
- **THEN** `DownloadOptions.DownloadPath` SHALL fall back to its compiled-in default rather than the old flat value, since the old key is no longer bound

#### Scenario: Reference configuration reflects the new shape
- **WHEN** a developer consults `docker-compose.example.yml`
- **THEN** every example environment variable for download, rule-set, quality, and search settings SHALL use the new nested `FunkArr__<Domain>__<Property>` form
