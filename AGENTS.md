# funkarr

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
  FunkArr.Core/                   # Common types, Akka/Terp refs
  FunkArr.Api/                    # Internal REST API (Minimal APIs, OpenAPI-first)
  FunkArr.ArrApi/                 # Newznab + SABnzbd adapter (controller-based API)
  FunkArr.Search/                 # MediathekViewWeb query + release evaluation
  FunkArr.Download/               # Download pipeline, FFmpeg, subtitles, muxing
  FunkArr.RuleSet/                # Ruleset registry, models, GitHub sync, generator
  FunkArr.Scoring/                # Match scoring, stats, diagnostics
  FunkArr.History/                # Scoring history recording + stats aggregation
  FunkArr.Enrichment/             # TMDB + TVDB metadata enrichment
  FunkArr.Feed/                   # RSS feed ingest + feed store
  FunkArr.Messages/               # Commands, queries, responses (all domains)
  FunkArr.Persistence/            # DTOs (extend-only, versioned)
  FunkArr.UI/                     # Vue.js frontend (Vite + Tailwind)
  FunkArr.Search.Tests/
  FunkArr.Download.Tests/
  FunkArr.RuleSet.Tests/
  FunkArr.Scoring.Tests/
  FunkArr.History.Tests/
  FunkArr.Enrichment.Tests/
  FunkArr.Feed.Tests/
  FunkArr.Api.Tests/
  FunkArr.ArrApi.Tests/
  FunkArr.Architecture.Tests/     # ArchUnit conventions
  FunkArr.Tests.Shared/           # Shared test infrastructure
```

## Architecture guardrails

- **Domain isolation.** Domain projects (Search, Download, RuleSet, Scoring, History,
  Enrichment, Feed) must not reference each other. Communication only through Messages.
- **Reference direction:** Host -> Api/ArrApi -> Domains -> Core -> Messages/Persistence.
  Never upward, never cross-domain.
- **Core bundles framework refs.** FunkArr.Core references Messages + Persistence and
  declares Akka/Terp NuGet packages. Domain projects reference only Core.
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
├─ StatsCollector (non-persistent)                                [History]
├─ FeedCrawlManager (persistent, IWithTimers)                     [Feed]
│  └─ FeedHarvestActor (child, long-lived, Akka.Streams pipeline)
└─ FeedStoreManager (persistent, IWithTimers)                     [Feed]

SHARD REGIONS (Sharded Entity)
├─ TvSearchWorker (non-persistent, per search request)            [Search]
├─ MovieSearchWorker (non-persistent, per search request)         [Search]
├─ TvEvaluationWorker (non-persistent, per evaluation)            [Search]
├─ MovieEvaluationWorker (non-persistent, per evaluation)         [Search]
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

**NSubstitute** for mocking plain interfaces (e.g. `IDataFiles`, `IHttpClientFactory`)
instead of hand-rolled stub classes. Akka actors are never mocked this way - use
`TestProbe`/real actors (see `funkarr-test` skill). `Substitute.For<T>()` +
`sub.Member(Arg.Any<...>()).Returns(...)`; verify with `sub.Received(1).Method(...)`.

## Test assertion conventions

- **No `!.` in tests**: Never use the null-forgiving operator to access nullable
  properties. Use `Assert.NotNull(value)` first, then access properties cleanly.
  Exception: XLinq `Attribute("x")!.Value` is fine.
- **`Assert.Single` for single-element collections**: Use
  `var item = Assert.Single(collection)` instead of `collection[0]`. Asserts
  count and binds the element.
- **Count guard before indexing**: When accessing multiple elements by index,
  assert the count first: `Assert.Equal(2, list.Count)` before `list[0]`/`list[1]`.
- **No vacuous assertion after an already-proven value**: Once a value's type
  and non-null state are already proven (e.g. by `ExpectMsg<T>()`), the
  following assertion must inspect its actual content (property values,
  message text, counts), not just `Assert.NotNull(result)` again. A
  `Assert.NotNull(value)` used as a null guard before further property
  assertions is still correct and not vacuous.
- **Endpoint-registration tests assert real behavior**: A test for `Map<X>Api`
  must send a request to at least one mapped route and assert on the response
  (status code and/or body), not only that `IEndpointRouteBuilder.DataSources`
  is non-empty.
- **Assert the exact computed value, not just existence.** When behavior
  produces a concrete value (a string, a count, a computed field), assert on
  that value directly instead of settling for "it's not null" or "it's not
  empty."
- **Exceptions: capture and inspect, not just the type.**
  `var ex = Assert.Throws<T>(...)` / `await Assert.ThrowsAsync<T>(...)`, then
  assert on `ex`'s actual fields (`ex.ParamName`, `ex.Message`, custom
  properties) — not just the exception type.
- **Mock-call assertions specify exact args and count** —
  `mock.Received(1).Method(Arg.Is<T>(x => ...))`, not just "was called" /
  "didn't throw."
- **`Assert.IsType<T>(obj, exactMatch: false)`** to prove interface/base-type
  conformance instead of an unchecked cast.
- **No `Thread.Sleep`/arbitrary delay for async or eventually-consistent
  state.** Use a bounded wait with a descriptive timeout failure instead.
  Akka actor tests already get this for free via `TestProbe.ExpectMsg`/
  `AwaitAssert`.
- **`Assert.Multiple`** for grouped assertions on the same object/outcome, so
  one run reports every failing assertion instead of stopping at the first.
- **`Assert.Equal(expected, actual)` over `Assert.True(actual == expected)`**
  — better failure messages.
- Prefer `Theory`/`InlineData`/`MemberData` for small input/output sweeps;
  prefer separate named `[Fact]`s when each case needs a large distinct
  fixture (e.g. an embedded JSON payload) — a readable failure name beats
  DRY in that case.

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

## API conventions (FunkArr.Api)

### Endpoint files
- One static class per feature area: `{Feature}ApiEndpoints.cs`.
- One extension method `Map{Feature}Api(this WebApplication app)` per class.
- All handlers are inline lambdas. No `private static` handler methods
  (method group references prohibited). If a lambda grows too large, the
  logic belongs in the domain layer, not in a helper method.
- Constants (timeouts, JsonSerializerOptions) are `private static readonly`
  fields on the class — that is the only non-lambda content.
- Exception: `SystemApiEndpoints` setup checks are co-located with the
  endpoint (self-contained health probes, no domain actor).

### Endpoint lambda shape
Every handler follows: (1) resolve actor(s) from `IActorRegistry`,
(2) Ask with a message (`request.ToMessage()`/`request.ToCommand()` if
mapping needed), (3) switch on response → `IResult`. No business logic,
no multi-actor orchestration in the lambda body.

### Route paths
- Groups: `/api/{feature}` (plural for collections: `downloads`,
  `rulesets`; singular where natural: `mediathek`, `feed`, `system`,
  `setup`).
- Sub-resources use consistent nesting: queue operations under `/queue/`,
  history under `/history/`.
- Actions on a resource: `POST /resource/{id}/action`.
- Collection-level actions: `POST /resource/action`.
- Root of a group uses `"/"`, not `""`.

### Request and response models
- Organized in `Models/{Feature}/` subfolders: Downloads, RuleSets,
  Scoring, Feed, Mediathek, System, Setup, Shared. Namespace stays
  `FunkArr.Api.Models` (subfolders are for physical organization only).
- Shared types (`MediaType`, `OperationResult`, `ErrorResponse`) live
  in `Models/Shared/`.
- Enums live with the feature that owns them, not in a central `Enums.cs`.
- A file contains either request records or response records, not both
  (unless matched pair for a single endpoint).

### Enums over strings
- Request/response records use enum types for all known finite values.
  Never `string` for a value from a known set.
- API enums serialize as integers (no global `JsonStringEnumConverter`).
  Per-type string converters remain on SABnzbd enums and disk-model enums.
- Query parameters that represent enums bind directly as the enum type.
- Only exception: values passed through 1:1 to an external API (e.g.
  MediathekViewWeb `SortBy`/`SortOrder`).
- Response records never use `.ToString()` on enum values.
- `DateTimeOffset` stays `DateTimeOffset`, never `.ToString("o")`.
  `TimeOnly` stays `TimeOnly`, never `.ToString("HH:mm")`.

### Mapping extensions
- Live in `Extensions/{Feature}MappingExtensions.cs`.
- `ToApi()` for domain → API model, `ToMessage()`/`ToCommand()` for
  API → domain.
- Enum mapping via exhaustive switch (per enum-conventions rules).
- These are the only non-inline code the endpoint layer calls besides
  `IActorRegistry.GetAsync` and `actor.Ask`.

## Akka conventions

- **No `ContinueWith`**: Never use `Task.ContinueWith()` in actor code. Use
  `PipeTo` with `success:` and `failure:` parameters. `ContinueWith` bypasses
  Akka's threading guarantees.
- **No `Status.Failure`**: Never use Akka's `Status.Failure`. Use project-owned
  `XxxFailed(Exception Cause)` records. In `PipeTo`, always provide
  `failure: ex => new XxxFailed(ex)`.
- **No `EventStream`**: Never use `EventStream.Publish`/`Subscribe`.
  Use explicit actor references (Tell/Ask via registry or constructor injection).
- **Passivation via ShardOptions only** (cluster sharding): Use
  `ShardOptions { PassivateIdleEntityAfter = ... }` in `AkkaSetupContainer`.
  Never use `Context.SetReceiveTimeout` + manual `Passivate` in sharded entity
  actors.
- **Passivation via Passivate message** (local entity regions): Entity actors
  signal completion with `Context.Parent.Tell(new Passivate(PoisonPill.Instance))`.
  `EntityRegionOptions.PassivationTimeout` acts as safety net for stuck entities.
  Same protocol as cluster sharding — entities migrate without code changes.
- **Entity ID interfaces** — three layers, bottom-up:
  - `Terp.IWithId` — `Guid Id`. Use when the entity ID is a Guid.
  - `Terp.Akka.IWithEntityId` — `string EntityId`. Routing key for both
    `EntityIdExtractor` (local) and `ShardMessageExtractor` (cluster).
  - `IWith*Id` (FunkArr) — domain-specific typed property (`Guid DownloadId`,
    `string RuleSetId`). The primary access point in code. Each `IWith*Id`
    inherits `IWithEntityId` (and `IWithId` when the ID is a Guid) with
    default interface implementations:
    ```csharp
    public interface IWithDownloadId : IWithId, IWithEntityId
    {
        Guid DownloadId { get; }
        Guid IWithId.Id => DownloadId;
        string IWithEntityId.EntityId => DownloadId.ToString();
    }
    public interface IWithRuleSetId : IWithEntityId
    {
        string RuleSetId { get; }
        string IWithEntityId.EntityId => RuleSetId;
    }
    ```
  - `Id` and `EntityId` are explicit interface implementations — invisible on
    the message, only accessible via cast (which only extractors do).
  - Message records are unchanged: `record InitDownload(Guid DownloadId, ...) : IWithDownloadId;`
- **One `IWith*Id` per region** — each interface is a routing address, not a
  data type. Two regions with different purposes always get separate interfaces,
  even when the ID value is the same (e.g. `IWithRuleSetId` for RuleSetWorker,
  `IWithHistoryId` for HistoryWorker — both carry a ruleset ID string, but are
  separate routing contracts).
  **Exception: domain-internal correlation.** Within a single domain, one
  `IWith*Id` may span multiple regions when the ID is a correlation ID for a
  multi-actor flow (e.g. `IWithSearchId` correlates TvSearchWorker,
  MovieSearchWorker, TvEvaluationWorker, MovieEvaluationWorker — all Search
  domain). The caller (e.g. SearchManager) knows which region to address; the
  shared interface expresses that these actors participate in the same operation.
  Cross-domain sharing is never allowed.
- **Local entity regions** (`WithEntityRegion<TKey>` from Terp.Akka): Use for
  request-scoped or short-lived entities that don't need cluster distribution.
  Same entity pattern as shard regions (ID-based routing, passivation, message
  extractor) but local to the node. Use `EntityIdExtractor` (works with
  `IWithEntityId`). Entity actors self-passivate after completing their work.
- **Actor DI**: `resolver.Props<T>()` for actors needing DI deps.
  `Context.GetActor<T>()` for resolving other actors at runtime. Never pass
  `IActorRef` as constructor params.
- **Actor naming**: `*Manager` (Cluster Singleton), `*Worker` (Sharded Entity
  or Local Entity Region), `*Actor` (everything else - pools, plain actors).
- **PersistenceId convention**: Singletons use a literal
  (`public override string PersistenceId => "feed-crawl";`). Entity actors
  (sharded or local region) use the C# 14 `field` keyword — the getter
  includes the prefix, the constructor assigns the raw entity ID:
  ```csharp
  public override string PersistenceId => $"download-{field}";
  // constructor: PersistenceId = entityId;
  ```
- **Actor state pattern** (Pathfinder pattern): State in its own `<Actor>State.cs`
  file as a `sealed class` (not nested, not a record). Void instance methods
  `Apply(event)` mutate state in-place — all public mutation methods must be
  named `Apply` (overloaded by parameter type), never `ApplyFoo`/`ApplyBar`.
  When the actor needs to pass primitives or multiple values, introduce a small
  wrapper record next to the state class (same file, namespace level) and use
  `Apply(WrapperRecord)`. Private helper methods inside an Apply dispatcher
  may use any name. Mutable collections (`List<T>`, `Dictionary<K,V>`,
  `HashSet<T>`) — Akka's single-threaded mailbox guarantees safe mutation.
  `Empty` is a property (`public static XState Empty => new();`), never a
  shared static field. Actors are thin plumbing (message routing, persistence,
  lifecycle only). All mutable domain state lives in the state object — no
  leaked fields on actors (dictionaries, flags, counters). Infrastructure refs
  (actor refs, CTS, stopwatches) may stay on the actor. State never sent to
  callers directly - use `GetSnapshot()` -> response record, `FromSnapshot()`
  -> reconstruct. For persistent actors: `GetPersistenceState()` ->
  `Persisted*State` record in `FunkArr.Persistence/Events/`,
  `FromPersistence()` -> reconstruct.
  `SaveSnapshot(_state.GetPersistenceState())`, not `SaveSnapshot(_state)`.
  No Event/Dto suffix on record names, no custom serializer.
- **Persist / DeferAsync / SaveSnapshot**: Three distinct tools, each with one job:
  - `Persist(evt, callback)` - writes event to journal, stashes incoming commands
    until callback runs. State update goes in the callback:
    `Persist(evt, e => { _state.Apply(e); })`.
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
            _state.Apply(e);
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
            _state.Apply(e);
            Sender.Tell(new PauseCompleted());
        });
    }
    ```
  - **Never**: unconditional `SaveSnapshot` inside every `Persist` callback
    (doubles I/O per command).
- **Persistence mapping in domain projects**: `*MappingExtensions.cs` for
  Messages<->Persistence conversion lives in the domain project, not in Core.
  Core references Messages + Persistence but doesn't contain mapping logic.

## Terp & Terp.Akka usage

Use:
- **Packages** - Terp (ISetup<T> in namespace Terp.Hosting, Resilience,
  Diagnostics), Terp.Akka (depends on Terp).
- **Setup startup** - per-domain setup containers implementing
  ISetup<IServiceCollection> (and ISetup<WebApplication> for endpoints);
  applied in Program.cs with builder.Setups().Add<T>() before Build and
  app.Setups().Add<T>() after Build. Order matters: Logging and Telemetry
  first, ApplicationSetupContainer last in app.Setups().
- **Actor system** - AkkaSetupContainer derives from ActorSystemSetup
  (ActorSystemName, BuildSystem); one actor system per host.
- **Actor resolution** - `context.GetActor<T>()`, `context.GetActorAsync<T>()`,
  `context.ResolveChildActor<T>()` (Terp.Akka).
- **Backoff** - `props.BackoffOnFailure(...)` (Terp.Akka) and
  `Terp.Resilience.Backoff.Create(...).DelayWithJitter(...)`.

Do NOT use:
- `HandlerRegistry` - use `Receive<T>()` directly.
- `ActorRef<T>` - use `IActorRef` + `IActorRegistry`.
- Concurrency utils (`NamedSemaphoreSlimStore`, etc.).

## C# conventions

- Allman style braces (opening brace on new line).
- 4 spaces indentation, no tabs.
- Private fields prefixed with underscore `_fieldName`. PascalCase for
  public/protected members.
- Use `var` when the type is apparent.
- No `this.` qualifier unless necessary.
- Sort `using` statements with `System.*` first.
- `sealed` by default, `record` for messages/DTOs, nullable enabled everywhere.
- No XML docs. Code speaks through naming.
- Never use `async void`, `.Result`, or `.Wait()` - these cause deadlocks.
- Always pass `CancellationToken` through async call chains.
- `TimeProvider` everywhere, never `DateTime.Now` or `DateTime.UtcNow` directly.
- `dotnet format` enforced - run after editing `.cs` files. CI rejects violations.
- **Never modify `.editorconfig`** to silence naming or style warnings. The
  editorconfig defines the project's rules — fix the code to match, not the
  other way around. `const` fields use PascalCase (all accessibilities),
  private instance fields use `_camelCase`.

## Enum conventions

- **Persistence enums have explicit `int` values.** Every member of every enum in
  `FunkArr.Persistence` is assigned a value (`Show = 0, Movie = 1`), starting at 0.
  Values are a stored contract: never change, reuse, or reorder them (new enums start
  with `Unknown = 0`, see below; existing ones are not renumbered). New members get
  a new, unused value (gaps are fine, e.g. `PersistedSearchSource.Feed = 4`).
- **Enum-to-enum mapping uses exhaustive `switch` expressions** with
  `_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)`. Never convert
  between enum types with numeric casts (`(Target)(int)source`). Applies to
  Messages <-> Persistence and Api <-> Messages. Nullable enums: `x?.ToPersistence()`.
- A missing switch arm is a compile error (CS8509 under warnings-as-errors).
- **Every new enum starts with an explicit `Unknown = 0` member**, in every project
  (Api, ArrApi, Messages, Persistence). `default(T)` is then detectably "unset", and
  unrecognized external values bind to `Unknown` instead of failing binding (no automatic
  400). API handlers answer `Unknown` with the protocol's own error and reject it before
  mapping; enum-to-enum mapping `switch` expressions throw for `Unknown` via the
  `_ => throw` arm. Applies to enums added from now on; existing Persistence enums keep
  their stored values (`Show = 0`) and are not renumbered.
- **No `[JsonStringEnumMemberName]` on API enums.** API enums serialize as integers.
  Never put serialization attributes on `FunkArr.Messages` or `FunkArr.Persistence` enums.

## Warnings

- `TreatWarningsAsErrors` is on for all projects (`src/Directory.Build.props`). The build
  must stay at 0 warnings; fix the cause, don't add `NoWarn`.
- Tests pass `TestContext.Current.CancellationToken` to every call that accepts a
  `CancellationToken` (xUnit1051). The analyzer's code fix can apply it:
  `dotnet format analyzers src/FunkArr.slnx --diagnostics xUnit1051 --severity info`.

## Metrics conventions

- **Pattern**: Each domain has a static `Telemetry` class with `internal static
  readonly Meter` named `FunkArr.<Domain>` and `internal static readonly`
  instruments. No base class, no abstraction.
- **Registration**: All meters registered in `TelemetrySetupContainer.cs` via
  `.AddMeter("FunkArr.<Domain>")`. Export via Prometheus.
- **Naming**: `funkarr.<domain>.<metric_name>` with snake_case. Counters end
  with `_total`, time histograms end with `_seconds`.
- **Tags**: Short lowercase names (`reason`, `source`, `type`, `api`, `status`,
  `phase`). Keep cardinality bounded - never use unbounded IDs or query strings.
- **ObservableGauge**: Static backing field with `internal static void Set*()`
  setter. Actor sets the value; gauge callback reads it.
- **Instrument at operation boundary**: Record metrics where the operation
  completes (success or failure), not at the call site.

### Meters by domain

| Domain     | Meter               | Instruments |
|------------|---------------------|-------------|
| Download   | `FunkArr.Download`  | completed, failed, duration, bytes, enqueued, cancelled, retries, move_failed, queue_size, active, paused, schedule_enabled |
| Search     | `FunkArr.Search`    | requests, matches, no_match, duration, evaluations, evaluation_duration, timeouts, failed, mediathek_errors, results_per_request |
| Scoring    | `FunkArr.Scoring`   | accepted, rejected, duration, runs, regex_timeouts, no_config |
| Enrichment | `FunkArr.Enrichment`| requests, failed, duration, items_enriched, cache_entries |
| History    | `FunkArr.History`   | recordings, trimmed, queries |
| Feed       | `FunkArr.Feed`      | crawls, probes_unchanged, items_seen, unmatched, published, rejected, gaps, crawl_duration, entries |
| RuleSet    | `FunkArr.RuleSet`   | loaded, removed, active, update_checks, updates_applied, update_errors, validation_errors, scans, file_events |
| Host       | `FunkArr.ExternalApi`| requests, duration (HTTP client delegating handler) |

## Dev environment

Run via `docker compose -f docker-compose.dev.yml up -d --build`, never `dotnet run`.
Ports: FunkArr 6969, Sonarr 8989, Radarr 7878, Prowlarr 9696. Frontend uses pnpm.

## References

- MediathekViewWeb API: https://mediathekviewweb.de/
- Newznab API spec: https://newznab.readthedocs.io/
- SABnzbd API spec: https://sabnzbd.org/wiki/advanced/api
