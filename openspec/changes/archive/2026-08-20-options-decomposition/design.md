# Design: Options Decomposition

## Context

`FunkArrOptions` (`src/FunkArr/Configuration/FunkArrOptions.cs`) is bound once from
the flat `FunkArr` config section and injected as `IOptions<FunkArrOptions>` into
every consumer that needs *any* configuration value — actors
(`DownloadQueueActor`, `SearchActor`, `RuleSetRegistryActor`, `MatchLedgerActor`),
services (`QualityProbeService`, `GitHubReleaseClient`), and endpoints
(`SetupEndpoints`, `SabnzbdEndpoints`, `QueueEndpoints`, three `*ApiKeyFilter`
classes). A consumer that only needs `QualityProbeLimit` (e.g. `SearchActor`)
still depends on the full 21-property surface, including Postgres credentials
and rule-set refresh settings it never reads. This makes it hard to see, from a
constructor signature alone, which settings actually affect a given component,
and it means any config change anywhere invalidates `IOptions<FunkArrOptions>`
for every consumer.

## Goals

- Group related settings into domain-scoped options classes so constructor
  signatures document their real dependencies.
- Preserve existing `appsettings.json` / environment-variable behavior as much
  as possible; where the shape must change (flat → nested), keep the
  transition mechanical and documented.
- Keep per-section validation so invalid config still fails fast at startup
  (`ValidateOnStart()`), with error messages scoped to the section that failed.
- Touch every consumer exactly once — no interim state where some consumers
  read the old flat options and others read the new nested ones.

## Non-goals

- No change to *values* of any setting (defaults, ranges, semantics).
- No change to the Newznab/SABnzbd-facing API surface.
- `Prowlarr` and `ArrInstances` (on the current `FunkArrOptions`, consumed only
  by `SetupEndpoints`) are **not** relocated by this change. They don't fit any
  of the four new domains and are not part of the appsettings.json-bound 17
  properties (they're populated via the setup wizard's `ConfigFileWriter`
  merge, not static config). They stay on `FunkArrOptions` for now; a future
  `ArrOptions` can be split out separately if/when that surface grows.

## Decisions

### 1. Target shape

Split the current 21 properties of `FunkArrOptions` into five classes:

| Class | Section | Properties |
|---|---|---|
| `FunkArrOptions` (slimmed) | `FunkArr` | `ApiKey`, `LogFormat`, `PersistencePath`, `Postgres`, `MatchLedgerCapacity`, `Prowlarr`, `ArrInstances` |
| `DownloadOptions` | `FunkArr:Download` | `DownloadPath`, `TempPath`, `ConcurrentDownloads`, `PathMapping` |
| `RuleSetOptions` | `FunkArr:RuleSet` | `SourceUrl`, `Repository`, `Version`, `RefreshMode`, `Path`, `RefreshIntervalMinutes` |
| `QualityOptions` | `FunkArr:Quality` | `Probing`, `CacheTtlMinutes`, `CacheCapacity` |
| `SearchOptions` | `FunkArr:Search` | `QualityProbeLimit` |

Property names inside the new classes drop the domain prefix
(`RuleSetSourceUrl` → `RuleSetOptions.SourceUrl`) since the class itself now
carries that context, mirroring how `PostgresOptions` already drops the
`Postgres` prefix. Cross-cutting/bootstrap concerns (API auth, log sink
format, persistence backend selection, match-ledger sizing, arr connections)
stay on `FunkArrOptions` because they either gate startup wiring
(`FunkArrActorSystemSetup` needs `Postgres`/`PersistencePath` before any actor
exists) or are used by every API surface (`ApiKey`).

`QualityProbeLimit` is intentionally split off `QualityOptions` into its own
`SearchOptions` class rather than folded in with the other three `Quality*`
properties: it governs `SearchActor`'s per-search HEAD-request budget (a
search-path concern), while `Probing`/`CacheTtlMinutes`/`CacheCapacity`
govern `QualityProbeService`'s cache lifecycle (a caching concern). Keeping
them apart means `SearchActor` doesn't pull in cache-tuning knobs it never
reads, and `QualityProbeService` doesn't pull in the probe-limit knob it
never reads either — `QualityProbeService` decides *whether/how* to probe;
`SearchActor` decides *how many* results to hand it.

### 2. Config binding shape

Each new class binds to a nested section under `FunkArr`, using
`IConfiguration.GetSection("FunkArr:Download")` etc. — the standard
`AddOptions<T>().Bind(section).ValidateOnStart()` pattern already used for
`FunkArrOptions`, just pointed at a child section instead of the root.

```json
{
  "FunkArr": {
    "ApiKey": "",
    "LogFormat": "text",
    "PersistencePath": "data/funkarr.db",
    "MatchLedgerCapacity": 10000,
    "Postgres": { "...": "..." },
    "Download": {
      "DownloadPath": "/media/downloads",
      "TempPath": "data/temp",
      "ConcurrentDownloads": 3,
      "PathMapping": null
    },
    "RuleSet": {
      "SourceUrl": "https://raw.githubusercontent.com/rundfunkarr/rundfunkarr/main/data/rulesets.json",
      "Repository": "st0o0/funkarr",
      "Version": "latest",
      "RefreshMode": "github-release",
      "Path": "data/rulesets",
      "RefreshIntervalMinutes": 60
    },
    "Quality": {
      "Probing": true,
      "CacheTtlMinutes": 360,
      "CacheCapacity": 50000
    },
    "Search": {
      "QualityProbeLimit": 30
    }
  }
}
```

This is a **breaking shape change** for anyone setting `FunkArr__DownloadPath`
directly (flat env var) — the new path is `FunkArr__Download__DownloadPath`.
We accept the break rather than add a compatibility shim (see next
decision), because:

- The project is pre-1.0 and the only documented consumer of these env vars
  is `docker-compose.example.yml`, which this change updates in lockstep.
- A silent dual-binding shim (bind both `FunkArr:X` and `FunkArr:Download:X`
  into the same property) doubles the section's surface area and creates
  ambiguity about which wins if both are set — exactly the kind of implicit
  behavior this decomposition is trying to remove.
- .NET config binding has no built-in "alias a flat key into a nested
  section" mechanism; achieving it would require hand-written pre-bind
  transformation of `IConfiguration`, adding complexity disproportionate to
  a pre-1.0 internal config surface.

Compatibility is instead handled as **documentation**: `docker-compose.example.yml`
is updated to the new nested env var names, and a migration note is added
covering the old → new mapping for anyone with an existing `.env` file.

### 3. Per-section validators

Replace the single `FunkArrOptionsValidator` with one `IValidateOptions<T>`
per new class, each registered independently:

- `FunkArrOptionsValidator` (slimmed) — `ApiKey` non-empty, `LogFormat` in
  `{json, text}`, `Postgres.User`/`Password` required when `Postgres.Host` is
  set.
- `DownloadOptionsValidator` — `DownloadPath` non-empty, `ConcurrentDownloads`
  in `[1, 10]`.
- `RuleSetOptionsValidator` — no validation beyond binding today (current
  validator doesn't check RuleSet fields); kept as a no-op-success validator
  for symmetry and as a placeholder for future checks (e.g. `RefreshMode` in
  `{github-release, legacy-url}`).
- `QualityOptionsValidator` — `CacheTtlMinutes >= 1`, `CacheCapacity >= 100`.
- `SearchOptionsValidator` — `QualityProbeLimit >= 1`.

Each is registered via
`services.AddSingleton<IValidateOptions<TOptions>, TOptionsValidator>()` and
each options registration keeps `.ValidateOnStart()`, so a bad value in any
section still fails host startup with a section-scoped message (e.g.
`"FunkArr:Quality:CacheCapacity must be at least 100."`) rather than one giant
validator with mixed-domain error strings.

### 4. Consumer migration

Every current `IOptions<FunkArrOptions>` injection is replaced with the
specific option type(s) actually read, determined from usage:

| Consumer | Current | New |
|---|---|---|
| `ApiKeyFilter`, `SetupApiKeyFilter`, `MatchApiKeyFilter`, `QueueApiKeyFilter` | `IOptions<FunkArrOptions>` (`ApiKey`) | `IOptions<FunkArrOptions>` (unchanged — `ApiKey` stays put) |
| `FunkArrActorSystemSetup` | `IOptions<FunkArrOptions>` (`Postgres`, `PersistencePath`) | `IOptions<FunkArrOptions>` (unchanged) |
| `MatchLedgerActor` | `IOptions<FunkArrOptions>` (`MatchLedgerCapacity`) | `IOptions<FunkArrOptions>` (unchanged) |
| `GitHubReleaseClient` | `IOptions<FunkArrOptions>` (`RuleSetRepository`, `RuleSetVersion`) | `IOptions<RuleSetOptions>` |
| `RuleSetRegistryActor` | `IOptions<FunkArrOptions>` (`RuleSetPath`, `RuleSetRefreshMode`, `RuleSetSourceUrl`, `RuleSetRefreshIntervalMinutes`) | `IOptions<RuleSetOptions>` |
| `SearchActor` | `IOptions<FunkArrOptions>` (`QualityProbeLimit`) | `IOptions<SearchOptions>` |
| `QualityProbeService` | `IOptions<FunkArrOptions>` (`Probing`, `CacheTtlMinutes`, `CacheCapacity`) | `IOptions<QualityOptions>` |
| `DownloadQueueActor` | `IOptions<FunkArrOptions>` (`TempPath`, `DownloadPath`, `ConcurrentDownloads`) | `IOptions<DownloadOptions>` |
| `QueueEndpoints` | `IOptions<FunkArrOptions>` (`PathMapping`, `ApiKey`) | `IOptions<DownloadOptions>` + `IOptions<FunkArrOptions>` |
| `SabnzbdEndpoints` | `IOptions<FunkArrOptions>` (`ApiKey`, `DownloadPath`) | `IOptions<DownloadOptions>` + `IOptions<FunkArrOptions>` |
| `SetupEndpoints` | `IOptions<FunkArrOptions>` (nearly everything) | all five: `IOptions<FunkArrOptions>`, `IOptions<DownloadOptions>`, `IOptions<RuleSetOptions>`, `IOptions<QualityOptions>`, `IOptions<SearchOptions>` |

`SetupEndpoints` is the one place that legitimately needs (almost) every
option, because it powers the setup wizard's config read/write/test-connection
UI. That's expected — it's a cross-cutting admin surface, not evidence the
split failed.

### 5. `ConfigFileWriter`

`ConfigFileWriter`'s constructor takes `FunkArrOptions` only to read
`PersistencePath` (to derive the sibling `config.json` path). Since
`PersistencePath` stays on `FunkArrOptions`, `ConfigFileWriter` is unaffected.

## Risks / trade-offs

- **Breaking env var change.** Anyone with `FunkArr__DownloadPath` etc. set
  today must update to `FunkArr__Download__DownloadPath` on upgrade. Mitigated
  by updating `docker-compose.example.yml` and calling this out as a breaking
  change in the PR/changelog; acceptable given pre-1.0 status.
- **Five DI registrations instead of one.** More boilerplate in
  `FunkArrServiceSetup`, but each block is a mechanical 3-line
  `AddOptions().Bind().ValidateOnStart()` + validator registration — low risk,
  high readability payoff.
- **Test setup churn.** Any test that hand-constructs `IOptions<FunkArrOptions>`
  with a full 21-property object now needs to construct the specific narrower
  options type(s) instead. This is mechanical but touches every actor/service
  test that injects config.
