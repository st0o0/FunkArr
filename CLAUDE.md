# CLAUDE.md

## Project

FunkArr — German public broadcaster media library integration for the *arr
ecosystem. A .NET service (Docker container) on Akka.NET: searches ARD/ZDF/etc.
Mediatheken via MediathekViewWeb API, downloads video + subtitles, remuxes to
MKV via FFmpeg, and exposes Newznab-compatible indexer API (for Sonarr/Radarr/
Prowlarr) and SABnzbd-compatible download client API.

### Architecture guardrails

- **Single csproj with namespace-based layering.** No multi-project solution —
  features live in namespace folders: `Search/`, `DownloadClient/`, `Muxing/`,
  `Indexer/`, `Shared/`, `Configuration/`, `Persistence/`, `Health/`.
- **Servus AppBuilder startup.** Three setup containers:
  `FunkArrServiceSetup` (DI), `FunkArrActorSystemSetup` (actors + persistence),
  `FunkArrApplicationSetup` (Minimal API endpoints).
- **Naming convention:** `*Coordinator` (singletons/parents), `*Worker`
  (children), `*Tracker` (state-holding shard entities).
- **Actor hierarchy:** `SearchCoordinator` (cache + coalescing + PipeTo pipeline,
  5 workers: ShowResolver, MediathekGateway, Match, QualityProbe, Score),
  `RuleSetCoordinator` (index + RefreshWorker + MatchQualityWorker),
  `QueueCoordinator` (scheduling, MaxConcurrent, event-sourced),
  `DownloadCoordinator` (shard entity, stage machine, 5 transient workers),
  `DownloadRequestTracker` (shard entity, API-facing status).
- **Single-node Cluster Sharding.** Two ShardRegions: `DownloadCoordinator`
  (per-nzoId work engine) and `DownloadRequestTracker` (per-nzoId API status).
- **Persistence tiers.** T1 (critical, event-sourced): QueueCoordinator,
  DownloadCoordinator, DownloadRequestTracker. T2 (cache warmth,
  event-sourced + snapshots): ShowResolverWorker, MatchQualityWorker.
  T3 (ephemeral): everything else.
- **Persistence DTOs** (`FunkArr.Persistence`): extend-only. Never remove or
  rename a `[JsonProperty]` string. New properties must be nullable or have a
  default. Increment `Version` when semantics change.
- **Two API surfaces.** Newznab XML (indexer for Prowlarr) and SABnzbd JSON
  (download client for Sonarr/Radarr). Both authenticate via `ApiKey` query
  parameter.

## Build & test

All commands run from `src/` (where `global.json` lives):

```powershell
dotnet build FunkArr.slnx
dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj                                    # all tests
dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj -- -class "<FullyQualifiedName>"   # single class
```

Tests are xUnit v3 on Microsoft.Testing.Platform — `dotnet run`, **not** `dotnet test`.
Shared test infrastructure lives in `FunkArr.Tests.Shared`.

Run the service from `src/FunkArr/` (`dotnet run`). Configuration layers:
- `appsettings.json` — production defaults.
- `appsettings.Development.json` — dev overrides.
- Environment variables — Docker-compose (`FunkArr__ApiKey`, etc.).

## Conventions

- **Git: NEVER `git push`** — the user pushes. Commit messages are Conventional
  Commits (commitlint-enforced). Single-line only, no Co-Authored-By.
- Versioning is release-please. Never edit `<Version>` in
  `src/Directory.Build.props` by hand.
- Central package management with transitive pinning: versions live only in
  `src/Directory.Packages.props`; add packages via `dotnet add package`, never
  edit csproj XML for versions.
- C#: records for messages/DTOs, `sealed` by default, nullable enabled.
- Messages: nested `sealed record` inside owning actor (commands, queries,
  responses). Domain events in dedicated `*Events.cs` files per actor.
- Persistence DTOs (`FunkArr.Persistence`): extend-only. Never remove or rename
  a `[JsonProperty]` string. New properties must be nullable or have a default.
  Increment `Version` when semantics change. Recovery code must handle all
  versions >= 1.
- All artifacts, specs, and communication in English.

## Workflow

Changes go through OpenSpec: `/opsx:explore` to think -> `/opsx:propose` to create
a change (proposal/design/specs/tasks) -> `/opsx:apply` to implement -> `/opsx:archive`.

## Skill routing (invoke by name)

- Actors & supervision: `sepp:actor-pattern-library`, `sepp:resilience-patterns`
- Messages & pipeline design: `sepp:message-driven-designer`
- Domain modeling: `sepp:domain-modeling-patterns`
- State machines: `sepp:state-machine-designer`
- Code quality: `sepp:complexity-guardian`, `sepp:code-complexity-analyzer`
- Servus patterns: `servus-skills:servus-*` (startup, actors, handlers, etc.)

## References

- MediathekViewWeb API: https://mediathekviewweb.de/ (search endpoint)
- Newznab API spec: https://newznab.readthedocs.io/
- SABnzbd API spec: https://sabnzbd.org/wiki/advanced/api
- Reference project: D:\GIT\njord (same patterns, same author)
