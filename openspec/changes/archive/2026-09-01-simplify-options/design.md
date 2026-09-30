## Context

FunkArr uses four options classes across two projects with three independently configurable path settings. For a Docker-only service with a single `/data` volume mount, this is unnecessary complexity. Users should configure one root path and have everything else derived.

Current state:
- `FunkArrOptions` (in `FunkArr`): `ApiKey`, `PersistencePath`, `DownloadPath`
- `RuleSetUpdaterOptions` (in `Core`): `Repository`, `Version`, `RefreshEnabled`, `DataPath`
- `ScoringOptions` (in `Core`): `PoolSize`
- `MatchHistoryOptions` (in `Core`): `MaxSnapshots`, `MaxAgeDays`, `SnapshotInterval`

## Goals / Non-Goals

**Goals:**
- Single configurable path root (`DataPath`) — all storage paths derived from it
- One central location for path constants
- All options classes accessible from domain projects via `FunkArr.Core`

**Non-Goals:**
- Merging all options into one class — behavioral options stay in their domain classes
- Changing the config section hierarchy (`FunkArr:RuleSet:*` etc. stays)
- Adding validation beyond what exists today

## Decisions

### Move `FunkArrOptions` to `FunkArr.Core`

Domain projects (`RuleSet`, `Search`, etc.) need access to derived paths. Since `Core` is the shared dependency, `FunkArrOptions` belongs there. The host project already references `Core`.

Alternative: Keep in host, pass paths via constructor injection. Rejected — adds indirection for no benefit.

### Computed properties for derived paths

`FunkArrOptions` exposes read-only computed properties:

```
DataPath          = "data"                              (configurable)
PersistencePath   = Path.Combine(DataPath, "funkarr.db")  (computed)
DownloadPath      = Path.Combine(DataPath, "downloads")   (computed)
RuleSetDataPath   = Path.Combine(DataPath, "community")   (computed)
```

These are regular C# get-only properties using `Path.Combine`, not config-bound. The subdirectory names are fixed — no reason to make them configurable.

Alternative: Constants class with static paths. Rejected — `DataPath` needs to be configurable at runtime via config/env vars, so it must live on an options class.

### `RuleSetUpdaterOptions` loses `DataPath`

`RuleSetUpdater` currently reads `DataPath` from its own options. After this change, it resolves the path from `FunkArrOptions.RuleSetDataPath` instead. The `RuleSetUpdaterOptions` class keeps only behavioral settings (`Repository`, `Version`, `RefreshEnabled`).

### `DownloadApiEndpoints` uses `FunkArrOptions`

Currently reads `FunkArr:DownloadPath` directly from `IConfiguration`. After this change, injects `IOptionsMonitor<FunkArrOptions>` and reads `.DownloadPath`.

## Risks / Trade-offs

- [Breaking config] Users with custom `PersistencePath` or `DownloadPath` in their config will silently lose those overrides → Acceptable: project is 0.x, no external users yet
- [Path.Combine behavior] `Path.Combine("data", "funkarr.db")` produces relative paths; `Path.Combine("/data", "funkarr.db")` produces absolute → Works correctly for both dev (`data/`) and Docker (`/data/`) scenarios

## Open Questions

_(none)_
