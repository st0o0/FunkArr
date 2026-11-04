# Migration: Servus / Servus.Akka to Terp / Terp.Akka

Status: plan only, nothing migrated yet. Execute the steps in order, one commit per step
(single-line commit messages, see AGENTS.md). Every step ends with its acceptance check.

## 1. Summary, scope, prerequisites

FunkArr uses Servus 0.35.0 and Servus.Akka 0.3.15 for four things: the `AppBuilder` startup with
setup containers, the Akka registry helpers (`GetActor`, `GetActorAsync`), `BackoffOnFailure`, and
`Servus.Resilience.Backoff`. All of them have a Terp counterpart. Verified facts (checked against the sources):

- Servus is referenced only in `src/Directory.Packages.props` (lines 25-26) and `src/FunkArr.Core/FunkArr.Core.csproj`.
- No startup gates, no lifecycle hooks (`OnApplicationStarted/Stopping/Stopped`), no `ServusTrace`, no `Option<T>`/`ChildTell`/`GetChild`,
  no `Servus.Akka.Local` / `ActorRef<T>` usage. No tests use Servus (FunkArr.IntegrationTests builds its own host).
- No Servus type names in persisted data or HOCON, so no data migration.
- Out of scope: renaming the containers (`*SetupContainer` stays, Terp's `XxxSetup` naming is optional later), startup gates.

Prerequisites:

- Terp and Terp.Akka are published to NuGet (0.x, versions are independent per package: check both on nuget.org,
  do not assume equal numbers). Below `<Terp>` and `<TerpAkka>` stand for those versions.
- Renovate: the shared preset `st0o0/renovate-config:dotnet` groups `Terp` and `Terp.*`; `renovate.json` needs no change.
- Work on a branch. Never push (commits stay local).

| Old package | New package | Notes |
|-------------|-------------|-------|
| `Servus` 0.35.0 | `Terp` `<Terp>` | setups (`Terp.Hosting`), startup gates, Resilience, Diagnostics |
| `Servus.Akka` 0.3.15 | `Terp.Akka` `<TerpAkka>` | depends on `Terp` (>= its pack-time version, no hand-managed floor); still needs Akka.Hosting (already referenced) |

Two Terp packages: `Terp` (hosting setups and startup gates in `Terp.Hosting` / `Terp.Hosting.Gates`, plus Diagnostics, Resilience, Network, date helpers; depends only on the
Microsoft.Extensions Hosting/Logging abstractions) and `Terp.Akka` (depends on `Terp`). The ASP.NET Core `FrameworkReference` was transitive with Servus and is not with Terp:
`FunkArr` is `Microsoft.NET.Sdk.Web` and has it anyway; add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to any other project that used it only through Servus
and fails to compile after step 5 (for example on `WebApplication` or Minimal API types).

## 2. Mapping table

| Servus construct (FunkArr use) | Terp construct | Status | Files |
|---|---|---|---|
| `using Servus.Akka;` + `Context.GetActor<T>()` (31 uses) | `using Terp.Akka;`, same call | DIRECT (namespace only) | DownloadManager, DownloadScheduler, DownloadWorker, HistoryWorker, StatsCollector, RuleSetManager, RuleSetWorker, MovieSearchWorker, TvSearchWorker, SearchManager (10 files) |
| `Context.GetActorAsync<T>()` (1 use) | same, `Terp.Akka` | DIRECT | RuleSetUpdater |
| `.BackoffOnFailure(...)` | same signature, `Terp.Akka` | DIRECT | Configuration/AkkaSetupContainer.cs |
| `Servus.Resilience.Backoff.Create / BackoffPolicy.DelayWithJitter` | `Terp.Resilience`, same calls; new optional `Random? random` | DIRECT | DownloadWorker, MovieSearchWorker, TvSearchWorker |
| `ActorSystemSetupContainer` (`GetActorSystemName()`, `BuildSystem`) | `Terp.Akka.ActorSystemSetup` (`protected abstract string ActorSystemName { get; }`, `BuildSystem(AkkaConfigurationBuilder, IServiceProvider)`) | CHANGED (base class; method becomes property) | AkkaSetupContainer |
| `IServiceSetupContainer.SetupServices(services, configuration)` | `ISetup<IServiceCollection>.Setup(services, SetupContext context)`, use `context.Configuration` | CHANGED | Arr, Core, Download, Enrichment, Logging, RuleSet, Scoring, Search, Telemetry containers |
| `ApplicationSetupContainer<WebApplication>.SetupApplication(app)` (protected override) | `ISetup<WebApplication>.Setup(WebApplication app, SetupContext context)` (public), applied with `app.Setups().Add<T>()` | CHANGED | Application, Download, RuleSet, Search containers |
| `AppBuilder.Create(builder, b => b.Build()).WithSetup<T>()....Build()` | `builder.Setups().Add<T>()` chain, `builder.Build()`, `app.Setups().Add<T>()` | CHANGED | Program.cs |
| `runner.RunAsync()` | `app.RunAsync()` | CHANGED | Program.cs |
| Own `ResolveChildActorExtensions.ResolveChildActor` (Core) | `Terp.Akka.ResolveChildActor` (compatible overload) | REMOVED (own copy deleted) | FunkArr.Core, EnrichmentManager |
| `ExternalApiMetrics.cs` (own meter `FunkArr.ExternalApi`) | `TerpHttpMetricsHandler` + meter `System.Net.Http` | CHANGED, optional | ExternalApiMetrics.cs, Enrichment/SearchSetupContainer, TelemetrySetupContainer |
| Gates, lifecycle hooks, `ServusTrace` | `AddStartupGate<T>()` (`Terp.Hosting.Gates`), `IHostApplicationLifetime` | not used by FunkArr | n/a |
| `.WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))` (4 uses) | `.ResumeOnError()` (`using Terp.Akka;`) | DIRECT | DownloadManager, StatsCollector, RuleSetManager, MediathekViewWebManager |
| `FunkArr.Feed.StreamSupervision.LoggingDecider(log)` (1 use) | `.ResumeOnError(log)` (`using Terp.Akka;`) | DIRECT (delete `StreamSupervision.cs`) | FeedCrawlManager |

## 3. Step-by-step plan

### Step 1: add Terp packages (keep Servus for now)

Files: `src/Directory.Packages.props`, `src/FunkArr.Core/FunkArr.Core.csproj`. Do not edit XML by hand if you prefer the CLI:
`dotnet add src/FunkArr.Core/FunkArr.Core.csproj package Terp --version <Terp>` (and `Terp.Akka`), then check that CPM put the
version in `Directory.Packages.props` and not in the csproj.

```xml
<!-- Directory.Packages.props, next to the Servus lines -->
<PackageVersion Include="Terp" Version="<Terp>" />
<PackageVersion Include="Terp.Akka" Version="<TerpAkka>" />
<!-- FunkArr.Core.csproj, next to the Servus references -->
<PackageReference Include="Terp" />
<PackageReference Include="Terp.Akka" />
```

`Terp` replaces what `Servus` provided (setups, `Terp.Resilience`, later `Terp.Diagnostics` in step 6).
FunkArr.Core is referenced by every domain project, so the setup namespaces are available to the host (`Program.cs`, `Configuration/*`) through it.

Acceptance: `dotnet build src/FunkArr.slnx` succeeds (both libraries side by side compile because no file imports both namespaces).

### Step 2: namespace renames for helpers (no behaviour change)

In the 10 files listed in the mapping table (all `GetActor`/`GetActorAsync` users), replace `using Servus.Akka;` with `using Terp.Akka;`.
In DownloadWorker, MovieSearchWorker and TvSearchWorker also replace `using Servus.Resilience;` with `using Terp.Resilience;`.
Code bodies stay unchanged (`Backoff.Create(..., maxDelay: ...)`, `_backoff.DelayWithJitter(...)` compile as is).
Do not touch `Configuration/AkkaSetupContainer.cs` here (step 3). Then `dotnet format src/FunkArr.slnx` (fixes using order; IDE0005 removes unused usings).

Acceptance: `dotnet build src/FunkArr.slnx`; `dotnet run --project src/FunkArr.Search.Tests/FunkArr.Search.Tests.csproj` and
`dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj` (plus the other `*.Tests` projects, same command pattern);
`dotnet format src/FunkArr.slnx --verify-no-changes`; `grep -rn "using Servus" src` lists only `Configuration/*` and `Program.cs`.

### Step 3: ActorSystemSetup, all containers and Program.cs (one atomic block)

Not separable: `AkkaSetupContainer` can only derive from the Terp base class once `Program.cs` no longer feeds it to `WithSetup`.
Files: `src/FunkArr/Program.cs` and the 11 `src/FunkArr/Configuration/*SetupContainer.cs` files (not `ExternalApiMetrics.cs`, `RetryAfterDefaults.cs`).

Mechanical rules for every container:

1. Remove `using Servus.Application.Startup;` (and `using Servus.Akka;`, `using Servus.Akka.Startup;`), add `using Terp.Hosting;` (and `using Terp.Akka;` in the Akka container).
2. Base list: `IServiceSetupContainer` becomes `ISetup<IServiceCollection>`; `ApplicationSetupContainer<WebApplication>` becomes `ISetup<WebApplication>`.
3. `public void SetupServices(IServiceCollection services, IConfiguration configuration)` becomes `public void Setup(IServiceCollection services, SetupContext context)`;
   every use of `configuration` in the body becomes `context.Configuration`. Where the parameter was unused or `_`, write `SetupContext _`.
4. `protected override void SetupApplication(WebApplication app)` becomes `public void Setup(WebApplication app, SetupContext context)`
   (use `SetupContext _` if `context` is unused; it is unused in all four web containers).
5. Class names, namespaces and method bodies stay otherwise identical.

Where `configuration` is used: ArrApi (1x `AddArrApiServices(configuration)`), Core (3x `GetSection`), Download (2x), Logging (1x `ReadFrom.Configuration`),
RuleSet (1x), Scoring (2x). Not used: Search, Enrichment, Telemetry.

3a. Service-only container, e.g. `ScoringSetupContainer`:

```csharp
// before
using FunkArr.Core;
using Servus.Application.Startup;
public sealed class ScoringSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ScoringOptions>().Bind(configuration.GetSection(ScoringOptions.SectionName)).ValidateOnStart();
        ...
// after
using FunkArr.Core;
using Terp.Hosting;
public sealed class ScoringSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services.AddOptions<ScoringOptions>().Bind(context.Configuration.GetSection(ScoringOptions.SectionName)).ValidateOnStart();
        ...
```

(Keep the original multi-line formatting; the snippet is shortened.) `LoggingSetupContainer`: header as above, `.ReadFrom.Configuration(context.Configuration)`.
`EnrichmentSetupContainer` and `TelemetrySetupContainer`: header only, `SetupContext _`.

3b. Both kinds in one class, `DownloadSetupContainer` (same shape for `SearchSetupContainer`, `RuleSetSetupContainer`):

```csharp
// before
public sealed class DownloadSetupContainer : ApplicationSetupContainer<WebApplication>, IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration) { ...configuration.GetSection(...)... }
    protected override void SetupApplication(WebApplication app) => app.MapDownloadsApi();
}
// after
public sealed class DownloadSetupContainer : ISetup<IServiceCollection>, ISetup<WebApplication>
{
    public void Setup(IServiceCollection services, SetupContext context) { ...context.Configuration.GetSection(...)... }
    public void Setup(WebApplication app, SetupContext _) => app.MapDownloadsApi();
}
```

`builder.Setups().Add<DownloadSetupContainer>()` runs only the `IServiceCollection` part; the `WebApplication` part runs on `app.Setups().Add<T>()`.

3c. Application-only container, `ApplicationSetupContainer` (body of `SetupApplication` unchanged):

```csharp
// before
public sealed class ApplicationSetupContainer : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
// after
public sealed class ApplicationSetupContainer : ISetup<WebApplication>
{
    public void Setup(WebApplication app, SetupContext _)
```

It has no builder target, so it must only be passed to `app.Setups().Add`, never to `builder.Setups().Add` (that throws `InvalidOperationException`: no target).

3d. Actor system, `AkkaSetupContainer`. Change the usings, the base class and the name member; `BuildSystem` (including the actor registrations, the `BackoffOnFailure` call and
the Register* helpers) keeps its body, only its signature must match (`protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider provider)`):

```csharp
// before
using Servus.Akka.Startup;
using Servus.Akka;
public sealed class AkkaSetupContainer : ActorSystemSetupContainer
{
    protected override string GetActorSystemName() => "funkarr";
// after
using Terp.Akka;
public sealed class AkkaSetupContainer : ActorSystemSetup
{
    protected override string ActorSystemName => "funkarr";
```

All actors are registered inside `BuildSystem`; there are no separate contributor setups. `builder.Setups().Add<AkkaSetupContainer>()` applies it (it is an `ISetup<IServiceCollection>`).

3e. `Program.cs`. Real old code and new code (the alias `using ApplicationSetupContainer = ...` existed only to disambiguate from Servus and is removed):

```csharp
// before (excerpt)
using FunkArr.Configuration;
using Serilog;
using Servus.Application.Startup;
using ApplicationSetupContainer = FunkArr.Configuration.ApplicationSetupContainer;
...
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();

    var runner = AppBuilder.Create(builder, b => b.Build())
        .WithSetup<LoggingSetupContainer>()
        .WithSetup<TelemetrySetupContainer>()
        .WithSetup<CoreSetupContainer>()
        .WithSetup<AkkaSetupContainer>()
        .WithSetup<SearchSetupContainer>()
        .WithSetup<DownloadSetupContainer>()
        .WithSetup<ScoringSetupContainer>()
        .WithSetup<RuleSetSetupContainer>()
        .WithSetup<EnrichmentSetupContainer>()
        .WithSetup<ArrApiSetupContainer>()
        .WithSetup<ApplicationSetupContainer>()
        .Build();

    await runner.RunAsync();
```

```csharp
// after
using FunkArr.Configuration;
using Serilog;
using Terp.Hosting;
...
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();

    builder.Setups()
        .Add<LoggingSetupContainer>()
        .Add<TelemetrySetupContainer>()
        .Add<CoreSetupContainer>()
        .Add<AkkaSetupContainer>()
        .Add<SearchSetupContainer>()
        .Add<DownloadSetupContainer>()
        .Add<ScoringSetupContainer>()
        .Add<RuleSetSetupContainer>()
        .Add<EnrichmentSetupContainer>()
        .Add<ArrApiSetupContainer>();

    var app = builder.Build();

    app.Setups()
        .Add<SearchSetupContainer>()
        .Add<DownloadSetupContainer>()
        .Add<RuleSetSetupContainer>()
        .Add<ApplicationSetupContainer>();

    await app.RunAsync();
```

Order rules: `ApplicationSetupContainer` must stay last in `app.Setups()` (catch-all `/api/{**rest}` and `MapFallbackToFile`). The web order Search, Download, RuleSet,
Application is the old `WithSetup` order of the containers that were `ApplicationSetupContainer<WebApplication>`. Do not add `ScoringSetupContainer`, `ArrApiSetupContainer`,
`CoreSetupContainer` etc. to `app.Setups()` (they have no `ISetup<WebApplication>`; it would throw). `app.Setups().Add<X>()` creates a second instance of the three dual containers;
this is harmless (stateless). `try/catch/finally` with `Log.Fatal` and `Log.CloseAndFlushAsync` stays unchanged.

Acceptance: `dotnet build src/FunkArr.slnx`; all test projects (commands as in step 2) plus `src/FunkArr.IntegrationTests`, `src/FunkArr.Api.Tests`,
`src/FunkArr.Architecture.Tests` via `dotnet run --project <csproj>`; `dotnet format src/FunkArr.slnx --verify-no-changes`.
Smoke test: start the host via `docker compose -f docker-compose.dev.yml up -d --build` (AGENTS.md: never `dotnet run`), check `/healthz`, `/alive`, `/metrics`, `/scalar`, one `/api/...` endpoint from each of
Search/Download/RuleSet and an unknown `/api/nope` (expects 404 JSON, not index.html), and a non-API path (expects the SPA `index.html`).

### Step 4: remove the own `ResolveChildActorExtensions`

Terp's `ResolveChildActor<TActor>(this IActorContext, string? name, Func<Props, Props> configure, params object[] args)` has the same shape as the Core copy.
Both versions in scope make the call in `EnrichmentManager` ambiguous (CS0121), so do both edits in the same commit:

1. Delete `src/FunkArr.Core/ResolveChildActorExtensions.cs`.
2. In `src/FunkArr.Enrichment/EnrichmentManager.cs` add `using Terp.Akka;` (the two `Context.ResolveChildActor<...>("tvdb-pool", props => ...)` calls stay unchanged).
   Then `dotnet format` (drops `using FunkArr.Core;` if it is no longer needed).

Acceptance: build, `dotnet run --project src/FunkArr.Enrichment.Tests/FunkArr.Enrichment.Tests.csproj` (EnrichmentManagerTests), format check.

### Step 5: remove Servus

Delete `PackageVersion` lines `Servus` and `Servus.Akka` from `src/Directory.Packages.props` and the two `PackageReference` lines from `src/FunkArr.Core/FunkArr.Core.csproj`.
CI runs with `locked-mode: false`, so there is no lock file to regenerate.

Acceptance: `grep -rni servus src` finds nothing (ignoring bin/obj); build, all tests, format check as in step 3.

### Step 6 (optional): replace `ExternalApiMetrics.cs` with `TerpHttpMetricsHandler`

Do this only if changing the metric names (dashboards, alerts) is acceptable; it is independent of steps 1-5.

1. `EnrichmentSetupContainer.cs`: two lines. Same in `SearchSetupContainer.cs` (one line).
   ```csharp
   // before
   .AddHttpMessageHandler(() => new ExternalApiMetricsHandler("tvdb"))
   // after (add using Terp.Diagnostics;)
   .AddHttpMessageHandler(() => new TerpHttpMetricsHandler("tvdb"))
   ```
   Likewise `"tmdb"` and `"mediathekviewweb"`. Keep the handler before `AddStandardResilienceHandler`, as today.
2. `TelemetrySetupContainer.cs`: replace `.AddMeter("FunkArr.ExternalApi")` with `.AddMeter("System.Net.Http")`.
3. Delete `src/FunkArr/Configuration/ExternalApiMetrics.cs` (contains `ExternalApiMetrics` and `ExternalApiMetricsHandler`).

Metric change (Prometheus names via the OTel exporter):

| Old | New |
|---|---|
| `funkarr_external_api_requests_total{api,status_code="2xx\|4xx\|5xx\|error"}` | `http_client_request_duration_seconds_count{api,http_response_status_code,error_type,...}` |
| `funkarr_external_api_duration_seconds_{bucket,sum,count}{api}` | `http_client_request_duration_seconds_{bucket,sum,count}{api,...}` |

PromQL equivalents (filter `api!=""` because `System.Net.Http` also reports untagged clients such as GitHub and ArrSetupClient):

```promql
# request rate by api and status class (old: rate(funkarr_external_api_requests_total[5m]))
sum by (api, http_response_status_code) (rate(http_client_request_duration_seconds_count{api!=""}[5m]))
# error ratio (old: status_code="error" or 5xx share); connection failures have error_type set
sum by (api) (rate(http_client_request_duration_seconds_count{api!="", error_type!=""}[5m]))
  / sum by (api) (rate(http_client_request_duration_seconds_count{api!=""}[5m]))
# p95 latency (old: histogram_quantile(0.95, rate(funkarr_external_api_duration_seconds_bucket[5m])))
histogram_quantile(0.95, sum by (le, api) (rate(http_client_request_duration_seconds_bucket{api!=""}[5m])))
```

Differences: the BCL meter measures each HTTP attempt on the wire (retries of the resilience handler count separately; the old handler counted one logical call
including retries), status is the exact code instead of a class (group with `http_response_status_code=~"5.."`), and the bucket layout is the BCL default.
A request cancelled before it was sent is not recorded. Acceptance: build, tests, format check; `/metrics` contains `http_client_request_duration_seconds_count{api="tmdb",...}` after one enrichment call.

4. `Download/Telemetry.cs` and `RuleSet/Telemetry.cs`: replace `ObservableGauge<T>` + backing field + setter with BCL `Gauge<T>` (.NET 9+).
   ```csharp
   // before
   private static int _queueSize;
   internal static void SetQueueSize(int size) => _queueSize = size;
   _meter.CreateObservableGauge("funkarr.download.queue_size", () => _queueSize, description: "Current download queue depth");
   // ...
   SetQueueSize(42);

   // after
   internal static readonly Gauge<int> QueueSize = Meter.CreateGauge<int>("funkarr.download.queue_size", description: "Current download queue depth");
   // ...
   Telemetry.QueueSize.Record(42);
   ```
   Applies to all 5 gauges: `queue_size`, `active`, `paused`, `schedule_enabled` (Download) and `active` (RuleSet).
   No backing field, no setter, no thread-safety concerns — `Gauge<T>.Record` is thread-safe internally.
   The `paused`/`schedule_enabled` bool-to-int pattern (`paused ? 1 : 0`) stays at the call site.
   Metric names and semantics are unchanged; dashboards and alerts do not need updating.
   Requires OTel SDK 1.10.0+ for `Gauge<T>` export support.

5. All `.Add(1)` / `.Add(-1)` counter calls can use `Terp.Diagnostics.CounterExtensions`:
   ```csharp
   // before
   Telemetry.Scans.Add(1);
   Telemetry.Loaded.Add(1, new KeyValuePair<string, object?>("source", sourceType));

   // after (add using Terp.Diagnostics;)
   Telemetry.Scans.Up();
   Telemetry.Loaded.Up(new("source", sourceType));
   ```
   Optional cleanup; no metric-name change.

### Step 7: replace stream supervision boilerplate with `ResumeOnError`

Replace the four `.WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))` call sites with `.ResumeOnError()`:

- `FunkArr.Download/DownloadManager.cs` (line ~142)
- `FunkArr.History/StatsCollector.cs` (line ~38)
- `FunkArr.RuleSet/RuleSetManager.cs` (line ~255)
- `FunkArr.Search/MediathekViewWebManager.cs` (line ~46)

Replace the `StreamSupervision.LoggingDecider(log)` call site with `.ResumeOnError(log)`:

- `FunkArr.Feed/FeedCrawlManager.cs` (line ~69)

Then delete `FunkArr.Feed/StreamSupervision.cs` (the logging decider is now in `Terp.Akka`).

Add `using Terp.Akka;` where not already present. Remove `using Akka.Streams.Supervision;` where `Deciders` was the only reason for the import.

Acceptance: `dotnet build`, all tests pass; `grep -rn "Deciders.ResumingDecider\|StreamSupervision" --include="*.cs" src/` finds nothing (except this file).

### Step 8: docs and specs

See section 5. Acceptance: `grep -rni servus AGENTS.md CLAUDE.md openspec/specs openspec/config.yaml` finds nothing; archive and `CHANGELOG.md` stay untouched.

## 4. Behaviour differences and risks

- Ordering. `builder.Setups().Add<T>()` runs immediately, in call order (old: all containers collected, then run grouped by kind: logging, configuration, services, then
  application setups). Inside one class implementing several targets the order is fixed: configuration, logging, services, metrics, builder. FunkArr has only service and
  web targets, so the call order in Program.cs is the old service order. Logging and Telemetry stay first; `AkkaSetupContainer` stays after `CoreSetupContainer`
  (its `BuildSystem` resolves `PostgresOptions`, `DataPaths`, `IDataFiles` lazily from the provider, so only registration, not call order, matters).
- Web setups now run in the `app.Setups().Add<T>()` order written in Program.cs (see step 3e). A wrong order is silent: a catch-all route registered too early swallows later endpoints.
- A setup that matches no target throws at `builder.Setups().Add` (`ApplicationSetupContainer` passed to the builder) or at `app.Setups().Add` (service-only container passed to the app).
- Exactly one actor system per host: Terp throws `InvalidOperationException` if a second `ActorSystemSetup` (or any `ActorSystem` registration) is applied.
  The name is validated eagerly at `Setup` time (ASCII letters/digits/`-`/`_`, starting with letter or digit); `"funkarr"` is fine.
  Old Servus called `AddAkka` without validation.
- `ActorSystemSetup.Setup` is not virtual and no longer a public `SetupServices`; subclasses override only the `ActorSystemName` property (was `GetActorSystemName()`) and `BuildSystem`.
- `GetActor<T>` throws `MissingActorRegistryEntryException` if nothing is registered (as before); `GetActorAsync<T>` waits until registered and never throws
  for a missing entry; `TryGetActor` returns `false` and `ActorRefs.Nobody`. Same semantics as Servus.Akka, now documented.
- `Backoff`: overflow-safe `DelayWithJitter` (Servus threw when the base delay was `TimeSpan.MaxValue`; FunkArr always sets `maxDelay`, so no effect) and an injectable
  `Random` (`Backoff.Create(..., random: new Random(42))`) for deterministic tests. Jitter range is unchanged (0.75 to 1.25, capped at `maxDelay`).
- `BackoffOnFailure` defaults and signature are unchanged (3s/30s, 0.2, unlimited retries; the container overrides via arguments and `configure`).
- Startup: `AppRunner.RunAsync` also printed the exception to the console before rethrowing; now only the existing `Log.Fatal` in Program.cs reports it.
  The old runner registered no lifetime callbacks for FunkArr, so nothing is lost.
- `Terp` has no ASP.NET Core dependency; `WebApplication`-typed setups are fine because FunkArr is `Microsoft.NET.Sdk.Web`. The builder target is matched by the static receiver type:
  `builder.Setups()` preserves the concrete `WebApplicationBuilder` type across the chain.
- Package versions are 0.x: API changes may be breaking. Pin exact versions via CPM (as now) and let Renovate propose updates.

### Order of setups

Setups are applied only by explicit calls: `builder.Setups().Add<T>()` runs immediately, in call order, and `app.Setups().Add<T>()` does the same after `Build()`.
Inside one class that implements several targets the order is fixed: configuration, logging, services, metrics, builder. There is no discovery and no ordering attribute.
FunkArr keeps its current explicit order: Logging and Telemetry first, Akka after Core, `ApplicationSetupContainer` last in `app.Setups()` (catch-all route).

## 5. Text updates

Replacement wording uses "Terp setup" for containers; class names are unchanged.

AGENTS.md
- Line 23: `# Common types, Akka/Servus refs` becomes `# Common types, Akka/Terp refs`.
- Line 54: `declares Akka/Servus NuGet packages` becomes `declares Akka/Terp NuGet packages`.
- Lines 272-288, section `## Servus & Servus.Akka usage`: rename to `## Terp & Terp.Akka usage` and replace the "Use" list with:
  - `**Packages** - Terp (ISetup<T> in namespace Terp.Hosting, Resilience, Diagnostics), Terp.Akka (depends on Terp).`
  - `**Setup startup** - per-domain setup containers implementing ISetup<IServiceCollection> (and ISetup<WebApplication> for endpoints); applied in Program.cs with builder.Setups().Add<T>() before Build and app.Setups().Add<T>() after Build. Order matters: Logging and Telemetry first, ApplicationSetupContainer last in app.Setups().`
  - `**Actor system** - AkkaSetupContainer derives from ActorSystemSetup (ActorSystemName, BuildSystem); one actor system per host.`
  - `**Actor resolution** - context.GetActor<T>(), context.GetActorAsync<T>(), context.ResolveChildActor<T>() (Terp.Akka).`
  - `**Backoff** - props.BackoffOnFailure(...) (Terp.Akka) and Terp.Resilience.Backoff.Create(...).DelayWithJitter(...).`
  - Drop the `WithResolvableActors`, `GetChild`/`ChildTell`/`ChildForward` and `Option<T>` bullets (not used in FunkArr; the first is not Terp API at all). Keep the "Do NOT use" list.
- Meters table row `Host | FunkArr.ExternalApi | requests, duration (HTTP client delegating handler)`: only with step 6, change to `Host | System.Net.Http | http.client.request.duration (tag api, via TerpHttpMetricsHandler)`.
- Line 311 (metrics registration in `TelemetrySetupContainer.cs`): unchanged, unless step 6 is done; then add one sentence: `External API calls use Terp's TerpHttpMetricsHandler (meter "System.Net.Http", metric http.client.request.duration with tag api).`

CLAUDE.md
- Line 43: `| Servus setup containers | akka-skills:setup-container |` becomes `| Terp setup containers (ISetup<T>) | akka-skills:setup-container |`.

openspec/config.yaml
- Line 13: `Servus + Servus.Akka (AppBuilder startup, actor registration/resolution, safe child communication, Option<T>, ServusTrace)` becomes
  `Terp + Terp.Akka (ISetup<T> startup, ActorSystemSetup, actor registry helpers, BackoffOnFailure, Backoff)`.
- Line 29: `Servus AppBuilder startup (3 setup containers: Service, ActorSystem, Application)` becomes `Terp ISetup<T> startup (setup containers per domain, applied via builder.Setups().Add<T>() / app.Setups().Add<T>())`.

Active specs under `openspec/specs` (edit in place via the normal openspec change flow if you use one; archive is never touched)
- `application-bootstrap/spec.md` line 3 (`Servus AppBuilder` to `Terp ISetup startup`); line 7 heading `Servus AppBuilder startup` to `Terp setup startup`;
  line 8 replace `AppBuilder.Create(builder, b => b.Build())` ... `.WithSetup<T>()` with: containers applied via `builder.Setups().Add<T>()` in the listed order (1 to 10),
  then `builder.Build()`, then `app.Setups().Add<T>()` for Search, Download, RuleSet, Application in that order; line 21 `The AppBuilder chain and await runner.RunAsync()` to
  `The builder.Setups() chain and await app.RunAsync()`; lines 34 and 38 `AppBuilder chain` to `builder.Setups() chain`; lines 153 and 165 `ApplicationSetupContainer.SetupApplication` to `ApplicationSetupContainer.Setup(WebApplication, SetupContext)`.
- `structured-logging/spec.md` lines 9, 11, 38, 45: `IServiceSetupContainer` to `ISetup<IServiceCollection>`, `AppBuilder chain` to `builder.Setups() chain`.
- `logging-setup-container/spec.md` line 3 (`Servus setup container` to `Terp setup`), lines 7-8 (`as IServiceSetupContainer`/`implement IServiceSetupContainer` to `ISetup<IServiceCollection>`),
  line 16 (`AppBuilder chain` to `builder.Setups() chain`), lines 23-24 (`first .WithSetup<T>() call in the AppBuilder chain` to `first builder.Setups().Add<T>() call`).
- `setup-container-layout/spec.md` line 5 (`Servus setup containers` to `Terp setup containers`), lines 62 and 66 (`AppBuilder chain in Program.cs` to `builder.Setups() chain in Program.cs`);
  the order sentence on line 66 gets `then app.Setups(): Search, Download, RuleSet, Application`. Lines 14 and 41 mention `WithResolvableActors` and already do not match the code; leave or fix separately.
- `backoff-calculation/spec.md` lines 3 and 5: `Servus BackoffPolicy` / `Servus.Resilience.BackoffPolicy` to `Terp BackoffPolicy` / `Terp.Resilience.BackoffPolicy`.
- `download-messages/spec.md` line 273: `via Servus actor registry` to `via the Terp.Akka actor registry helpers`.
- `project-structure/spec.md` lines 22, 24, 26: `Servus, and Servus.Akka` to `Terp, and Terp.Akka`; `Akka/Servus` to `Akka/Terp`; `Servus types` to `Terp types`.
- `solution-infrastructure/spec.md` line 42: `Servus, Servus.Akka` to `Terp, Terp.Akka`.
- `ruleset-api/spec.md` line 93 and `static-file-serving/spec.md` line 37: `ApplicationSetupContainer.SetupApplication` to `ApplicationSetupContainer.Setup(WebApplication, SetupContext)`.
- Only with step 6: `prometheus-metrics-endpoint/spec.md` lines 83-95 (requirement "External API health metrics"): replace the two metric names with
  `http.client.request.duration` (Prometheus `http_client_request_duration_seconds`) tagged `api` and `http_response_status_code`; and in `docs/observability.md` +
  `docs/en/observability.md` replace the table rows at lines 83-88 and the PromQL at line 127 with the queries from step 6.

Untouched: `openspec/changes/archive/**`, `CHANGELOG.md`, `E2E-TEST-PLAN.md` (no Servus mention).

Separate follow-up, outside this repository: the Claude skill `akka-skills:setup-container` (D:\GIT\akka-skills, `skills/`) still documents Servus `AppBuilder` /
`IServiceSetupContainer` and must be rewritten for `ISetup<T>`; until then treat its examples as outdated and follow this guide and AGENTS.md.

## 6. Verification checklist and rollback

After step 5 (and again after 6/7 if done):

- [ ] `dotnet build src/FunkArr.slnx` without warnings that CI treats as errors.
- [ ] `dotnet format src/FunkArr.slnx --verify-no-changes`.
- [ ] All test projects via `dotnet run --project src/<Name>.Tests/<Name>.Tests.csproj` (Search, Download, Enrichment, History, RuleSet, Scoring, Persistence, ArrApi, Api, Architecture) and `src/FunkArr.IntegrationTests`.
- [ ] Packages: `Terp` and `Terp.Akka` referenced (the `Terp.Hosting` namespace comes from `Terp`).
- [ ] `grep -rni servus src AGENTS.md CLAUDE.md openspec/specs openspec/config.yaml` is empty.
- [ ] Host starts (docker compose dev stack) with SQLite (default) and with Postgres env config; log shows actor system `funkarr` starting, no `InvalidOperationException` from Terp.
- [ ] `/healthz` 200 (Akka liveness/readiness and journal/snapshot health checks present), `/alive`, `/metrics` has `funkarr_*` meters, OpenAPI/Scalar, SPA fallback, unknown `/api/x` returns 404.
- [ ] Docker image builds (`Dockerfile`), container reaches healthy state.
- [ ] Persistence unaffected: an existing database from the Servus build opens and recovers (no schema or type-name change was made).

Rollback: each step is one local commit, so `git revert <commit>` per step in reverse order works; steps 1-2 and 4-5 are independent of 3 apart from the Servus package lines.
To abort the whole migration before merging, drop the branch. Since versions are pinned via CPM, going back to Servus 0.35.0 / Servus.Akka 0.3.15 is only a revert of the
step 1/5 package lines plus the code steps. Nothing is pushed until you do it yourself.

---

# Migration: options registration and validation with Terp

Separate from the Servus migration above (that plan is already executed: FunkArr references `Terp` 0.2.0). Moves FunkArr's options onto one path: `AddOptionsWithValidateOnStart<T[, TValidator]>().Bind(...)` in the setup container and an `IValidateOptions<T>` class using `Terp.Options.ValidationFailures`. Reference: Terp `docs/terp/options.md` and `Migration.md`.

## The rule

- Options are validated by a validator class next to the options type. No inline `.Validate(...)` in a setup container.
- Registration always through `AddOptionsWithValidateOnStart`, so no options type silently skips start validation.
- `Bind(context.Configuration.GetSection(X.SectionName))` stays in the setup container (the setup already has `SetupContext`).

## O0. Prerequisite

Bump `Terp` and `Terp.Akka` together in `src/Directory.Packages.props` (both are pinned to `0.2.0`) to the first Terp release that contains `Terp.Options.ValidationFailures`.

## O1. Registration per container

| Container | Options | Today | After |
| --- | --- | --- | --- |
| `CoreSetupContainer` | `FunkArrOptions` | `AddOptions` + `Bind` + `ValidateOnStart` | `AddOptionsWithValidateOnStart<FunkArrOptions>().Bind(...)` |
| `CoreSetupContainer` | `PostgresOptions` | `AddOptions` + `Bind`, **no** `ValidateOnStart` | `AddOptionsWithValidateOnStart<PostgresOptions>().Bind(...)`, if the omission was not intended |
| `CoreSetupContainer` | `RoutingOptions` | `AddOptions` + `Bind` + `ValidateOnStart` + separate `AddSingleton<IValidateOptions<...>>` | `AddOptionsWithValidateOnStart<RoutingOptions, RoutingOptionsValidator>().Bind(...)` |
| `DownloadSetupContainer` | `DownloadOptions` | same as Routing (separate `AddSingleton`) | `AddOptionsWithValidateOnStart<DownloadOptions, DownloadOptionsValidator>().Bind(...)` |
| `FeedSetupContainer` | `FeedOptions` | 8 inline `.Validate(...)` + `ValidateOnStart` | see O2 |
| `ScoringSetupContainer` | `ScoringOptions`, `VariantOptions`, `ScoringHistoryOptions` | `AddOptions` + `Bind` + `ValidateOnStart` | `AddOptionsWithValidateOnStart<T>().Bind(...)` each |
| `RuleSetSetupContainer` | `RuleSetUpdaterOptions` | `AddOptions` + `Bind` + `ValidateOnStart` | `AddOptionsWithValidateOnStart<RuleSetUpdaterOptions>().Bind(...)` |
| `EnrichmentSetupContainer` | `TvdbOptions`, `TmdbOptions` | `AddOptions` + `BindConfiguration`, **no** `ValidateOnStart` | `AddOptionsWithValidateOnStart<T>().Bind(context.Configuration.GetSection(...))` |

Notes:

- `EnrichmentSetupContainer` is the only container that uses `BindConfiguration` and ignores the context (`SetupContext _`). Rename the parameter to `context` and use `Bind(context.Configuration.GetSection(...))` like the others.
- `PostgresOptions`, `TvdbOptions` and `TmdbOptions` have no validator. `ValidateOnStart` then only surfaces binding errors at start instead of at first use. Decide per type whether that is wanted; if a type needs required keys (API keys), that is a reason to add a validator.

Before / after (`CoreSetupContainer`, routing):

```csharp
// before
services
    .AddOptions<RoutingOptions>()
    .Bind(context.Configuration.GetSection(RoutingOptions.SectionName))
    .ValidateOnStart();

services.AddSingleton<IValidateOptions<RoutingOptions>, RoutingOptionsValidator>();

// after
services
    .AddOptionsWithValidateOnStart<RoutingOptions, RoutingOptionsValidator>()
    .Bind(context.Configuration.GetSection(RoutingOptions.SectionName));
```

`Bind` stays in the container, not in a generic helper. This keeps the configuration binding source generator able to see the concrete type if FunkArr ever publishes with Native AOT.

## O2. Inline validation to a validator class

`FeedSetupContainer` is the only place with inline `.Validate(...)`.

```csharp
// before
services
    .AddOptions<FeedOptions>()
    .Bind(context.Configuration.GetSection(FeedOptions.SectionName))
    .Validate(o => o.Interval > TimeSpan.Zero, "FunkArr:Feed:Interval must be positive")
    .Validate(o => o.PageSize is > 0 and <= 1000, "FunkArr:Feed:PageSize must be between 1 and 1000")
    // ... 6 more rules
    .ValidateOnStart();

// after
services
    .AddOptionsWithValidateOnStart<FeedOptions, FeedOptionsValidator>()
    .Bind(context.Configuration.GetSection(FeedOptions.SectionName));
```

New file `src/FunkArr.Core/FeedOptionsValidator.cs`:

```csharp
using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core;

public sealed class FeedOptionsValidator : IValidateOptions<FeedOptions>
{
    public ValidateOptionsResult Validate(string? name, FeedOptions options)
    {
        var failures = new ValidationFailures(FeedOptions.SectionName);

        failures.Require(options.Interval > TimeSpan.Zero, "Interval must be positive");
        failures.Require(options.PageSize is > 0 and <= 1000, "PageSize must be between 1 and 1000");
        failures.Require(options.MaxPages > 0, "MaxPages must be positive");
        failures.Require(options.MaxCatchUp > TimeSpan.Zero, "MaxCatchUp must be positive");
        failures.Require(options.MaxConcurrency > 0, "MaxConcurrency must be positive");
        failures.Require(options.MinConfidence is >= 0 and <= 1, "MinConfidence must be between 0 and 1");
        failures.Require(options.Retention > TimeSpan.Zero, "Retention must be positive");
        failures.Require(options.InitialLookback > TimeSpan.Zero, "InitialLookback must be positive");

        return failures.ToResult();
    }
}
```

Messages are identical to today (`FunkArr:Feed:Interval must be positive`), because `FeedOptions.SectionName` is `"FunkArr:Feed"` and the validator now adds that prefix. Add `FeedOptionsValidatorTests` in `FunkArr.Feed.Tests` modelled on `DownloadOptionsValidatorTests`.

One difference: `.Validate(...)` registered one validator per rule. The class reports all failures in one result; the start-up exception lists them all either way.

## O3. Existing validators

`DownloadOptionsValidator` and `RoutingOptionsValidator` build a `List<string>` and end with the `Count > 0 ? Fail : Success` ternary. Their messages have **no** section prefix (`"Route definition has an empty Name."`, `"DownloadSchedule[{i}]: ..."`), and `DownloadOptionsValidatorTests` / `RoutingOptionsValidatorTests` assert on them. So use `new ValidationFailures()` **without** a section; the text stays the same.

```csharp
// DownloadOptionsValidator, before
var failures = new List<string>();

for (var i = 0; i < options.DownloadSchedule.Count; i++)
{
    var slot = options.DownloadSchedule[i];
    if (slot.Start == slot.End)
    {
        failures.Add($"DownloadSchedule[{i}]: Start and End must differ (both are {slot.Start}).");
    }
}

return failures.Count > 0
    ? ValidateOptionsResult.Fail(failures)
    : ValidateOptionsResult.Success;

// after
var failures = new ValidationFailures();

for (var i = 0; i < options.DownloadSchedule.Count; i++)
{
    var slot = options.DownloadSchedule[i];
    failures.Require(slot.Start != slot.End, $"DownloadSchedule[{i}]: Start and End must differ (both are {slot.Start}).");
}

return failures.ToResult();
```

`RoutingOptionsValidator` has `continue` branches and `else if` chains. Keep them and replace only the collector and the last statement:

```csharp
var failures = new ValidationFailures();
// body unchanged: the failures.Add("...") calls stay as they are
return failures.ToResult();
```

Only introduce a section prefix deliberately (for example `"FunkArr:Routing"`), and update both test files in the same commit if you do.

## O4. Order of work

1. Bump Terp.
2. Registration changes (O1), one commit. Behavior changes only where `ValidateOnStart` is newly enabled (`PostgresOptions`, `TvdbOptions`, `TmdbOptions`), so run the integration tests (`FunkArrFixture` binds options).
3. `FeedOptionsValidator` plus tests (O2).
4. Convert `DownloadOptionsValidator` and `RoutingOptionsValidator` (O3).

Suggested commits: `refactor(core): register options with AddOptionsWithValidateOnStart`, `refactor(feed): move feed options validation into FeedOptionsValidator`, `refactor(core): use ValidationFailures in options validators`.
