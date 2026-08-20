## Why

`FunkArrOptions` has 17 properties spanning 5 unrelated domains (download, ruleset, quality probing, persistence, match intelligence). Every service receives the entire config bag via `IOptions<FunkArrOptions>` even when it only needs 2-3 values. As features grow, this becomes a god-object config that obscures which settings affect which components.

## What Changes

- Split `FunkArrOptions` into domain-scoped options classes: `DownloadOptions`, `RuleSetOptions`, `QualityOptions`, `SearchOptions`
- Keep a slim `FunkArrOptions` for cross-cutting concerns (ApiKey, LogFormat) and bootstrap-only settings (PersistencePath, Postgres)
- Bind each section to a nested config path (e.g., `FunkArr:Download`, `FunkArr:RuleSet`)
- Add per-section validators replacing the single `FunkArrOptionsValidator`
- Update all consumers to inject the specific options type they need

## Capabilities

### New Capabilities
- `options-structure`: Domain-scoped configuration classes with nested appsettings binding. Sections: `Download` (paths, concurrency, path mapping), `RuleSet` (source, refresh, paths), `Quality` (probing, cache), `Search` (probe limit). Environment variable mapping preserved via ASP.NET section binding (`FunkArr__Download__ConcurrentDownloads`).

### Modified Capabilities

## Impact

- `FunkArr.Configuration.FunkArrOptions` — shrinks to ApiKey, LogFormat, PersistencePath, Postgres
- `FunkArr.Configuration.FunkArrServiceSetup` — binds new sections
- `FunkArr.Configuration.FunkArrOptionsValidator` — split into per-section validators
- All actors and services that inject `IOptions<FunkArrOptions>` — updated to inject specific options
- Test setup code — updated to provide domain-scoped options
- `appsettings.json` / `appsettings.Development.json` — restructured into nested sections
- Docker-compose env var examples — updated to nested paths
