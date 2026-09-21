# AGENTS.md

## Project

FunkArr - German-language public broadcaster media library integration for the *arr
ecosystem. A .NET service (Docker container) on Akka.NET: searches ARD/ZDF/ORF/SRF/etc.
Mediatheken via MediathekViewWeb API, downloads video + subtitles, remuxes to
MKV via FFmpeg, and exposes Newznab-compatible indexer API (for Sonarr/Radarr/
Prowlarr) and SABnzbd-compatible download client API.

## Language

All code, specs, docs, and communication in English. No German.

## Solution structure

Multi-project solution with domain isolation:

```
src/
  FunkArr.slnx
  FunkArr/                        # Host: Program.cs, Startup, DI, Config
  FunkArr.Core/                   # Common types, Akka/Servus refs
  FunkArr.Api/                    # Internal REST API (JSON, OpenAPI-first)
  FunkArr.ArrApi/                 # Newznab + SABnzbd adapter (thin translation)
  FunkArr.Search/                 # MediathekViewWeb query + fetch
  FunkArr.Download/               # Download pipeline, FFmpeg, subtitles, muxing
  FunkArr.RuleSet/                # Ruleset registry, models, GitHub sync, generator
  FunkArr.Scoring/                # Match scoring, stats, diagnostics
  FunkArr.History/                # Scoring history recording + stats aggregation
  FunkArr.Enrichment/             # TMDB + TVDB metadata enrichment
  FunkArr.Messages/               # Commands, queries, responses (all domains)
  FunkArr.Persistence/            # DTOs (extend-only, versioned)
  FunkArr.UI/                     # Vue.js frontend (Vite + Tailwind)
  FunkArr.Search.Tests/
  FunkArr.Download.Tests/
  FunkArr.RuleSet.Tests/
  FunkArr.Scoring.Tests/
  FunkArr.History.Tests/
  FunkArr.Enrichment.Tests/
  FunkArr.Api.Tests/
  FunkArr.ArrApi.Tests/
  FunkArr.Architecture.Tests/     # ArchUnit conventions
  FunkArr.Tests.Shared/           # Shared test infrastructure
```

## Build & test

All commands run from repo root (never cd into src/):

```powershell
dotnet build src/FunkArr.slnx
dotnet format src/FunkArr.slnx --verify-no-changes
```

Tests are xUnit v3 on Microsoft.Testing.Platform - `dotnet run`, **not** `dotnet test`:

```powershell
dotnet run --project src/FunkArr.Search.Tests/FunkArr.Search.Tests.csproj
dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj
```

## Message naming convention

- Commands: `VerbNoun` (e.g. `SearchSeries`, `AddDownload`). Response: `abstract record VerbNounResponse` with `VerbNounCompleted` / `VerbNounFailed`. Command + responses in one file.
- Queries: `QueryNoun` (e.g. `QueryRuleSetDetail`). Response: `abstract record NounResponse` with `NounResult` / `NounFailed`. Query + responses in one file.
- Events: Past-tense (e.g. `ScoringRecorded`), in `FunkArr.Persistence/Events/`.
- Config: Noun, fire-and-forget (e.g. `MatchingConfig`).
- No domain-wide response interfaces - each command/query has its own response type.

## Versioning

Version 0.x - breaking changes are fine without migration code or compatibility shims.
Revisit once version hits 1.0+.

## API path layout

- `/api` - Internal REST API for UI (FunkArr.Api, OpenAPI-first)
- `/index/api` - Newznab indexer API for Prowlarr/Sonarr/Radarr (FunkArr.ArrApi)
- `/download/api` - SABnzbd-compatible download client API (FunkArr.ArrApi)

## Akka conventions

- **No `Status.Failure`**: Never use Akka's `Status.Failure` as a message. Use project-owned
  `XxxFailed(Exception Cause)` records. In `PipeTo`, always provide `failure: ex => new XxxFailed(ex)`.
- **No `EventStream`**: Never use `EventStream.Publish`/`Subscribe` for inter-actor comms.
  Use explicit actor references (Tell/Ask via registry or constructor injection).
- **Actor DI**: `resolver.Props<T>()` for actors needing DI deps (HTTP clients, options).
  `Context.GetActor<T>()` for resolving other actors at runtime. Never pass `IActorRef` as
  constructor params.
- **Actor state pattern** (Pathfinder pattern): State in its own `<Actor>State.cs` file (not nested).
  Extension methods `Apply(event)` / `ProcessCommand(cmd)` on the state record. Actors are thin
  plumbing (message routing, persistence, lifecycle only). State never sent to callers directly -
  use `GetSnapshot()` → response record, `FromSnapshot()` → reconstruct. For persistent actors:
  `GetPersistenceState()` → `Persisted*State` record in `FunkArr.Persistence/Events/`,
  `FromPersistence()` → reconstruct. `SaveSnapshot(_state.GetPersistenceState())`, not
  `SaveSnapshot(_state)`. No Event/Dto suffix on record names, no custom serializer.

## Dev environment

Run via `docker compose -f docker-compose.dev.yml up -d --build`, never `dotnet run`.
Ports: FunkArr 6969, Sonarr 8989, Radarr 7878, Prowlarr 9696. Frontend uses pnpm.

## References

- MediathekViewWeb API: https://mediathekviewweb.de/
- Newznab API spec: https://newznab.readthedocs.io/
- SABnzbd API spec: https://sabnzbd.org/wiki/advanced/api
