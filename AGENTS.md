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
  FunkArr.Api/                    # Internal REST API (Minimal APIs, OpenAPI-first)
  FunkArr.ArrApi/                 # Newznab + SABnzbd adapter (controller-based API)
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

## Architecture guardrails

- **Domain isolation.** Domain projects (Search, Download, RuleSet, Scoring, History,
  Enrichment) must not reference each other. Communication only through Messages.
- **Reference direction:** Host -> Api/ArrApi -> Domains -> Core -> Messages/Persistence.
  Never upward, never cross-domain.
- **Core bundles framework refs.** FunkArr.Core references Messages + Persistence and
  declares Akka/Servus NuGet packages. Domain projects reference only Core.
- **ArrApi is a thin adapter.** Translates Newznab/SABnzbd protocols to domain
  commands/queries. No business logic.
- **ArchUnitNET** tests enforce reference direction, naming conventions, and domain
  boundaries.

## Actor topology

```
SINGLETONS (Cluster Singleton)
├─ MediathekViewWebManager (non-persistent, IWithUnboundedStash)  [Search]
│  └─ Manages MediathekViewWeb API connection state
├─ SearchManager (non-persistent, IWithTimers)                    [Search]
│  └─ Orchestrates search across shard regions
├─ RuleSetResolver (non-persistent)                               [RuleSet]
├─ RuleSetManager (non-persistent)                                [RuleSet]
├─ RuleSetUpdater (non-persistent, IWithTimers)                   [RuleSet]
├─ DownloadManager (persistent)                                   [Download]
├─ DownloadScheduler (non-persistent, IWithTimers)                [Download]
├─ DownloadHistoryManager (persistent)                            [Download]
├─ ScoringManager (non-persistent)                                [Scoring]
│  └─ ScoringActor pool (child, via Context.ActorOf router)
├─ EnrichmentManager (non-persistent)                             [Enrichment]
│  ├─ TvdbEnrichmentActor pool (child, via ResolveChildActor)
│  └─ TmdbEnrichmentActor pool (child, via ResolveChildActor)
└─ StatsCollector (non-persistent)                                [History]

SHARD REGIONS (Sharded Entity)
├─ TvSearchWorker (non-persistent, per search request)            [Search]
├─ MovieSearchWorker (non-persistent, per search request)         [Search]
├─ DownloadWorker (persistent, per download entity)               [Download]
├─ RuleSetWorker (non-persistent, per ruleset)                    [RuleSet]
└─ HistoryWorker (persistent, per scoring history entity)         [History]
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

- `/api` - Internal REST API for UI (FunkArr.Api, Minimal APIs, OpenAPI-first)
- `/index/api` - Newznab indexer API for Prowlarr/Sonarr/Radarr (FunkArr.ArrApi, controller-based API)
- `/download/api` - SABnzbd-compatible download client API (FunkArr.ArrApi, controller-based API)

## Akka conventions

- **No `ContinueWith`**: Never use `Task.ContinueWith()` in actor code. Use
  `PipeTo` with `success:` and `failure:` parameters. `ContinueWith` bypasses
  Akka's threading guarantees.
- **No `Status.Failure`**: Never use Akka's `Status.Failure`. Use project-owned
  `XxxFailed(Exception Cause)` records. In `PipeTo`, always provide
  `failure: ex => new XxxFailed(ex)`.
- **No `EventStream`**: Never use `EventStream.Publish`/`Subscribe`.
  Use explicit actor references (Tell/Ask via registry or constructor injection).
- **Passivation via ShardOptions only**: Use
  `ShardOptions { PassivateIdleEntityAfter = ... }` in `AkkaSetupContainer`.
  Never use `Context.SetReceiveTimeout` + manual `Passivate` in sharded entity
  actors.
- **MessageExtractor via interface**: All sharded messages implement `IWith*Id`
  (e.g. `IWithDownloadId`, `IWithSearchId`, `IWithRuleSetId`). One
  `ShardMessageExtractor` in Core handles all shard regions. Never match
  concrete message types in extractors.
- **Actor DI**: `resolver.Props<T>()` for actors needing DI deps.
  `Context.GetActor<T>()` for resolving other actors at runtime. Never pass
  `IActorRef` as constructor params.
- **Actor naming**: `*Manager` (Cluster Singleton), `*Worker` (Sharded Entity),
  `*Actor` (everything else - pools, plain actors).
- **Actor state pattern** (Pathfinder pattern): State in its own `<Actor>State.cs`
  file (not nested). Extension methods `Apply(event)` / `ProcessCommand(cmd)` on
  the state record. Actors are thin plumbing (message routing, persistence,
  lifecycle only). State never sent to callers directly - use `GetSnapshot()` ->
  response record, `FromSnapshot()` -> reconstruct. For persistent actors:
  `GetPersistenceState()` -> `Persisted*State` record in
  `FunkArr.Persistence/Events/`, `FromPersistence()` -> reconstruct.
  `SaveSnapshot(_state.GetPersistenceState())`, not `SaveSnapshot(_state)`.
  No Event/Dto suffix on record names, no custom serializer.
- **Persist / DeferAsync / SaveSnapshot**: Three distinct tools, each with one job:
  - `Persist(evt, callback)` - writes event to journal, stashes incoming commands
    until callback runs. State update goes in the callback:
    `Persist(evt, e => { _state = _state.Apply(e); })`.
  - `DeferAsync(evt, callback)` - runs callback **after all preceding Persist
    handlers complete**. Does NOT persist anything. The `evt` parameter is not
    persisted and not replayed on recovery - it is only passed to the callback
    as argument. `Sender` is preserved automatically (no `var sender = Sender`
    capture needed when using `Persist`, not `PersistAsync`).
  - `SaveSnapshot(_state.GetPersistenceState())` - for unbounded-growth actors
    (singletons, long-lived entities). **Interval-based**, never after every
    event. Use `LastSequenceNr % SnapshotInterval == 0`. Bounded-lifecycle
    entities (e.g. DownloadWorker with ~4 events) are fine without snapshots.
  - **When to use DeferAsync** - use when side-effects are non-trivial
    AND the handler does NOT call PersistAll/Persist internally:
    - Starting external processes (FFmpeg, HTTP calls)
    - Fan-out to 2+ actors (Tell to multiple recipients)
    - Response after nested Persist (DeferAsync runs after all Persists complete)
  - **Never use DeferAsync when** the handler calls DispatchNext or any method
    that calls Persist/PersistAll. PersistAll inside DeferAsync breaks the
    event batch pipeline - the inner events never reach the journal. Keep
    everything inline in the Persist callback instead.
  - **Inline in Persist callback is fine** when:
    - Handler uses DispatchNext or triggers further persistence
    - Only a single `Sender.Tell(response)` after state update
    - Logging
    - Metrics/Telemetry counters
  - Correct pattern (with DeferAsync):
    ```csharp
    private const int SnapshotInterval = 10;

    private void HandleCreate(CreateCmd cmd)
    {
        var evt = new PersistedCreated(cmd.Id, ...);
        Persist(evt, e =>
        {
            _state = _state.Apply(e);
            if (LastSequenceNr % SnapshotInterval == 0)
                SaveSnapshot(_state.GetPersistenceState());
        });
        DeferAsync("notify", _ =>
        {
            _manager.Tell(_state.GetRegistryUpdate());
            Sender.Tell(new CreateCompleted(_state.Id));
        });
    }
    ```
  - Correct pattern (simple response, inline):
    ```csharp
    private void HandlePause(PauseCmd cmd)
    {
        var evt = new PersistedPaused(...);
        Persist(evt, e =>
        {
            _state = _state.Apply(e);
            Sender.Tell(new PauseCompleted());
        });
    }
    ```
  - **Never**: unconditional `SaveSnapshot` inside every `Persist` callback
    (doubles I/O per command).
- **Persistence mapping in domain projects**: `*MappingExtensions.cs` for
  Messages<->Persistence conversion lives in the domain project, not in Core.
  Core references Messages + Persistence but doesn't contain mapping logic.

## Servus & Servus.Akka usage

Use:
- **AppBuilder startup** - per-domain setup containers. Shared:
  `ServiceSetupContainer` (Core DI), `LoggingSetupContainer`,
  `AkkaSetupContainer` (actors), `ApplicationSetupContainer` (app chrome).
- **Actor registration** - `WithResolvableActors(b => b.Register<T>("name"))`.
- **Actor resolution** - `context.GetActor<T>()`, `context.ResolveChildActor<T>()`.
- **Safe child communication** - `context.GetChild("name")` -> `Option<IActorRef>`,
  `context.ChildTell()`, `context.ChildForward()`.
- **`Option<T>` extensions** - `Match(some, none)` pattern.

Do NOT use:
- `HandlerRegistry` - use `Receive<T>()` directly.
- `LocalEntityRegion` - use Akka.NET Cluster Sharding.
- `ActorRef<T>` - use `IActorRef` + `IActorRegistry`.
- Concurrency utils (`NamedSemaphoreSlimStore`, etc.).

## C# conventions

- `sealed` by default, `record` for messages/DTOs, nullable enabled everywhere.
- No XML docs. Code speaks through naming.
- `dotnet format` enforced - run after editing `.cs` files. CI rejects violations.
- `TimeProvider` everywhere, never `DateTime.Now` or `DateTime.UtcNow` directly.

## Dev environment

Run via `docker compose -f docker-compose.dev.yml up -d --build`, never `dotnet run`.
Ports: FunkArr 6969, Sonarr 8989, Radarr 7878, Prowlarr 9696. Frontend uses pnpm.

## References

- MediathekViewWeb API: https://mediathekviewweb.de/
- Newznab API spec: https://newznab.readthedocs.io/
- SABnzbd API spec: https://sabnzbd.org/wiki/advanced/api
