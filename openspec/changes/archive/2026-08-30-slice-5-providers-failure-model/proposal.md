## Why

Three code paths reach the MediathekViewWeb API (MediathekGatewayActor, RuleSetGenerationService, RulesetController) — only one is throttled. Providers throw past their boundary and gateway actors swallow exceptions into null returns, losing failure details that would enable degraded-mode decisions. No retry or circuit breaker exists on any HTTP client. Five distinct non-actor type kinds exist (`*Service`, `*Client`, `*Controller`, `*Provider`, `*Helper`) with no distinguishing rule, so the same capability gets implemented twice and the copies drift.

## What Changes

- **Provider/Gateway pairing**: Move providers and their owning gateway actors into paired folders under `Providers/` — `Mediathek/`, `Tvdb/`, `Tmdb/`, `GitHub/`, `Ffmpeg/`, `FileSystem/`. One provider, one owning gateway, one folder.
- **IQueryResult hierarchy** (new, in Core): `QuerySuccess<T>`, `QueryFailure(FailureReason, Detail)` with `FailureReason` enum: `NotFound`, `Unauthorized`, `RateLimited`, `Timeout`, `Cancelled`, `Transport`, `Malformed`. Open hierarchy — every consumer `switch` carries a `default` arm.
- **Provider boundary enforcement**: Providers return `IQueryResult`, never throw. Gateway actors decide failure semantics (serve stale, degrade, fail).
- **Standard resilience on all HTTP clients**: `AddStandardResilienceHandler()` on `MediathekClient`, `TvdbV4Client`, `TmdbClient`, `GitHubReleaseClient`.
- **`*Service` suffix banned**: `RuleSetGenerationService` → Core generation logic + actor coordination. `SetupValidationService` → Core validation. `ApiKeyValidationService` → Core validation. `FfmpegService` → `FfmpegProvider`. `FileService` → `FileSystemProvider`.
- **Rename typed clients to `*Provider`**: `TvdbV4Client` → `TvdbProvider`, `TmdbClient` → `TmdbProvider`, `MediathekClient` → `MediathekProvider`, `GitHubReleaseClient` → `GitHubProvider`.

## Capabilities

### New Capabilities
- `query-result-model`: IQueryResult hierarchy and FailureReason enum in FunkArr.Core — the typed failure model for all provider boundaries.
- `provider-resilience`: Standard HTTP resilience configuration (retry, circuit breaker, timeout) applied uniformly to all typed HTTP clients.

### Modified Capabilities
- `tvdb-v4-client`: Renamed to `TvdbProvider`, returns `IQueryResult` instead of throwing, moves to `Providers/Tvdb/`.
- `tmdb-gateway-actor`: Gateway updated to handle `QueryFailure` from `TmdbProvider` (serve stale cache on failure).
- `tvdb-gateway-actor`: Gateway updated to handle `QueryFailure` from `TvdbProvider` (serve stale cache on failure).
- `mediathek-gateway-worker`: Gateway updated to handle `QueryFailure` from `MediathekProvider`. Renamed client.
- `ffmpeg-service`: `FfmpegService`/`IFfmpegService` renamed to `FfmpegProvider`/`IFfmpegProvider`, moves to `Providers/Ffmpeg/`.
- `file-operations`: `FileService`/`IFileService` renamed to `FileSystemProvider`/`IFileSystemProvider`, moves to `Providers/FileSystem/`.
- `github-release-refresh`: `GitHubReleaseClient` renamed to `GitHubProvider`, returns `IQueryResult`, moves to `Providers/GitHub/`.
- `setup-validation`: `SetupValidationService` and `ApiKeyValidationService` renamed — `*Service` suffix removed.
- `ruleset-auto-generation`: `RuleSetGenerationService` disbanded — generation logic stays in Core, orchestration moves to actor.

## Impact

- **Code**: All files under `Search/Resolvers/`, `Shared/FileService.cs`, `DownloadClient/Ffmpeg/`, `RuleSet/RuleSetGenerationService.cs`, `Setup/SetupValidation*.cs`, `Setup/ApiKeyValidation*.cs` move or rename.
- **Dependencies**: New package `Microsoft.Extensions.Http.Resilience` added to `Directory.Packages.props`.
- **DI**: All `AddHttpClient<T>()` registrations in `FunkArrServiceSetup.cs` updated for new type names and resilience handlers.
- **Tests**: Provider test fakes and actor test setups updated for new type names and `IQueryResult` return types.
- **API**: No external API changes — provider changes are internal.
