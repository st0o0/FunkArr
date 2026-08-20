# Tasks: Options Decomposition

## 1. New options classes

- [ ] 1.1 Create `src/FunkArr/Configuration/DownloadOptions.cs`: `SectionName = "FunkArr:Download"`, properties `DownloadPath` (default `/media/downloads`), `TempPath` (default `data/temp`), `ConcurrentDownloads` (default `3`), `PathMapping` (nullable, default `null`).
- [ ] 1.2 Create `src/FunkArr/Configuration/RuleSetOptions.cs`: `SectionName = "FunkArr:RuleSet"`, properties `SourceUrl` (default current URL), `Repository` (default `st0o0/funkarr`), `Version` (default `latest`), `RefreshMode` (default `github-release`), `Path` (default `data/rulesets`), `RefreshIntervalMinutes` (default `60`).
- [ ] 1.3 Create `src/FunkArr/Configuration/QualityOptions.cs`: `SectionName = "FunkArr:Quality"`, properties `Probing` (default `true`), `CacheTtlMinutes` (default `360`), `CacheCapacity` (default `50000`).
- [ ] 1.4 Create `src/FunkArr/Configuration/SearchOptions.cs`: `SectionName = "FunkArr:Search"`, property `QualityProbeLimit` (default `30`).
- [ ] 1.5 Slim `FunkArrOptions.cs` down to `ApiKey`, `LogFormat`, `PersistencePath`, `Postgres`, `MatchLedgerCapacity`, `Prowlarr`, `ArrInstances`. Remove the migrated properties.

## 2. Per-section validators

- [ ] 2.1 Rewrite `FunkArrOptionsValidator.cs` to validate only the remaining `FunkArrOptions` fields: `ApiKey` non-empty, `LogFormat` in `{json, text}`, `Postgres.User`/`Password` required when `Postgres.Host` is set.
- [ ] 2.2 Create `DownloadOptionsValidator.cs` (`IValidateOptions<DownloadOptions>`): `DownloadPath` non-empty, `ConcurrentDownloads` in `[1, 10]`.
- [ ] 2.3 Create `RuleSetOptionsValidator.cs` (`IValidateOptions<RuleSetOptions>`): starts as success-only placeholder (no checks existed previously); leave a comment noting future candidates (`RefreshMode` enum check).
- [ ] 2.4 Create `QualityOptionsValidator.cs` (`IValidateOptions<QualityOptions>`): `CacheTtlMinutes >= 1`, `CacheCapacity >= 100`.
- [ ] 2.5 Create `SearchOptionsValidator.cs` (`IValidateOptions<SearchOptions>`): `QualityProbeLimit >= 1`.

## 3. DI wiring

- [ ] 3.1 In `FunkArrServiceSetup.SetupServices`, replace the single `AddOptions<FunkArrOptions>()` block with five `AddOptions<T>().Bind(configuration.GetSection(T.SectionName)).ValidateOnStart()` blocks (one per class from section 1), each paired with its `AddSingleton<IValidateOptions<T>, TValidator>()` registration from section 2.

## 4. Update consumers

- [ ] 4.1 `GitHubReleaseClient` (`src/FunkArr/RuleSet/GitHubReleaseClient.cs`): inject `IOptions<RuleSetOptions>`; update field references (`RuleSetRepository` → `Repository`, `RuleSetVersion` → `Version`).
- [ ] 4.2 `RuleSetRegistryActor` (`src/FunkArr/RuleSet/RuleSetRegistryActor.cs`): inject `IOptions<RuleSetOptions>`; update field references (`RuleSetPath` → `Path`, `RuleSetRefreshMode` → `RefreshMode`, `RuleSetSourceUrl` → `SourceUrl`, `RuleSetRefreshIntervalMinutes` → `RefreshIntervalMinutes`).
- [ ] 4.3 `SearchActor` (`src/FunkArr/Search/SearchActor.cs`): inject `IOptions<SearchOptions>`; update `_probeLimit = options.Value.QualityProbeLimit`.
- [ ] 4.4 `QualityProbeService` (`src/FunkArr/Search/QualityProbeService.cs`): inject `IOptions<QualityOptions>`; update field references (`QualityProbing` → `Probing`, `QualityCacheTtlMinutes` → `CacheTtlMinutes`, `QualityCacheCapacity` → `CacheCapacity`).
- [ ] 4.5 `DownloadQueueActor` (`src/FunkArr/DownloadClient/DownloadQueueActor.cs`): inject `IOptions<DownloadOptions>`; update field references (`TempPath`, `DownloadPath`, `ConcurrentDownloads` — names unchanged, only the owning type changes).
- [ ] 4.6 `QueueEndpoints` (`src/FunkArr/DownloadClient/QueueEndpoints.cs`): inject both `IOptions<DownloadOptions>` (for `PathMapping`) and `IOptions<FunkArrOptions>` (for `ApiKey` in `QueueApiKeyFilter`); update `ParsePathMapping(options.Value.PathMapping)` call site.
- [ ] 4.7 `SabnzbdEndpoints` (`src/FunkArr/DownloadClient/SabnzbdEndpoints.cs`): inject both `IOptions<DownloadOptions>` (for `DownloadPath` in `HandleGetConfig`) and `IOptions<FunkArrOptions>` (for `ApiKey` in `ValidateApiKey`); thread the extra parameter through `HandleSabnzbdGet`/`HandleSabnzbdPost`/`HandleGetConfig`.
- [ ] 4.8 `SetupEndpoints` (`src/FunkArr/Configuration/SetupEndpoints.cs`): inject `IOptions<FunkArrOptions>`, `IOptions<DownloadOptions>`, `IOptions<RuleSetOptions>`, `IOptions<QualityOptions>`, `IOptions<SearchOptions>`; update every `opts.*` reference (test-connection paths, `HandleGetConfig` response shape, `ValidateApiKey`) to read from the correct options instance. Keep the JSON response shape consumers (setup wizard UI) rely on unchanged — regroup source, not payload shape, unless the UI is updated in the same change.
- [ ] 4.9 Confirm `ApiKeyFilter`, `SetupApiKeyFilter`, `MatchApiKeyFilter` (Indexer/RuleSet) keep `IOptions<FunkArrOptions>` unchanged (`ApiKey` didn't move) — no code change expected, just a sanity check.
- [ ] 4.10 Confirm `FunkArrActorSystemSetup` and `MatchLedgerActor` keep `IOptions<FunkArrOptions>` unchanged (`Postgres`, `PersistencePath`, `MatchLedgerCapacity` didn't move) — no code change expected, just a sanity check.
- [ ] 4.11 Confirm `ConfigFileWriter` constructor (uses `FunkArrOptions.PersistencePath`) is unaffected — no code change expected.

## 5. Config files

- [ ] 5.1 Restructure `src/FunkArr/appsettings.json`: nest `DownloadPath`/`TempPath`/`ConcurrentDownloads`/`PathMapping` under `FunkArr:Download`; nest `RuleSetSourceUrl`/`RuleSetRepository`/`RuleSetVersion`/`RuleSetRefreshMode` (stripped of prefix) under `FunkArr:RuleSet`; nest `QualityProbing`/`QualityCacheTtlMinutes`/`QualityCacheCapacity` (stripped of prefix) under `FunkArr:Quality`; nest `QualityProbeLimit` under `FunkArr:Search` as `QualityProbeLimit`; leave `ApiKey`, `PersistencePath`, `Postgres`, `LogFormat`, `MatchLedgerCapacity` at the `FunkArr` root.
- [ ] 5.2 Restructure `src/FunkArr/appsettings.Development.json` the same way (check its current content for any overrides that need the same nesting).

## 6. docker-compose example

- [ ] 6.1 Update `docker-compose.example.yml`: rewrite every `FunkArr__DownloadPath`, `FunkArr__TempPath`, `FunkArr__ConcurrentDownloads`, `FunkArr__PathMapping` to `FunkArr__Download__*`; every `FunkArr__RuleSet*` to `FunkArr__RuleSet__*` (stripped prefix); every `FunkArr__Quality*` to `FunkArr__Quality__*` (stripped prefix, except `QualityProbeLimit` which becomes `FunkArr__Search__QualityProbeLimit`); leave `FunkArr__ApiKey`, `FunkArr__LogFormat`, `FunkArr__PersistencePath`, `FunkArr__Postgres__*`, `FunkArr__MatchLedgerCapacity` unchanged.
- [ ] 6.2 Add a short comment noting the breaking rename from flat to nested env vars, for anyone upgrading an existing deployment.

## 7. Test setup

- [ ] 7.1 Search `src/FunkArr.Tests` and `src/FunkArr.Tests.Shared` for every place that constructs `IOptions<FunkArrOptions>` (directly or via `Options.Create`) and update each call site to construct the narrower options type(s) the consumer under test now requires — expected in at least `FunkArrOptionsValidatorTests.cs`, `RuleSetRegistryActorTests.cs`, `MatchLedgerActorTests.cs`, `GitHubReleaseClientTests.cs`, `QualityProbeServiceTests.cs`, `ConfigFileWriterTests.cs`, `FileServiceTests.cs`, `WebUiEndpointTests.cs`, `EndpointTests.cs`.
- [ ] 7.2 Add/rename validator test classes to match the new per-section validators (e.g. split `FunkArrOptionsValidatorTests.cs` content across `DownloadOptionsValidatorTests.cs`, `RuleSetOptionsValidatorTests.cs`, `QualityOptionsValidatorTests.cs`, `SearchOptionsValidatorTests.cs`, plus a slimmed `FunkArrOptionsValidatorTests.cs`).
- [ ] 7.3 Verify any shared test fixture/builder (`FunkArr.Tests.Shared`) that centrally builds config for integration tests binds all five sections correctly.

## 8. Verification

- [ ] 8.1 `dotnet build FunkArr.slnx` from `src/` — no warnings about unused `using Microsoft.Extensions.Options` or orphaned references to removed `FunkArrOptions` properties.
- [ ] 8.2 `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` from `src/` — full suite passes.
- [ ] 8.3 Manually start the service (`dotnet run --project src/FunkArr`) with a minimal `appsettings.Development.json` and confirm startup succeeds (validators pass) and the setup wizard's `get_config` endpoint returns the same JSON shape as before.
- [ ] 8.4 Grep the full `src/` tree for any remaining reference to the removed `FunkArrOptions` properties (`DownloadPath`, `TempPath`, `ConcurrentDownloads`, `PathMapping`, `RuleSetSourceUrl`, `RuleSetRepository`, `RuleSetVersion`, `RuleSetRefreshMode`, `RuleSetPath`, `RuleSetRefreshIntervalMinutes`, `QualityProbing`, `QualityCacheTtlMinutes`, `QualityCacheCapacity`, `QualityProbeLimit`) outside the new options classes themselves — should be zero hits.
