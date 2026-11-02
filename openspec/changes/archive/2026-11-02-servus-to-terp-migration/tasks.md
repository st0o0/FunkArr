## 1. Add Terp packages

- [x] 1.1 Add `Terp` and `Terp.Akka` PackageVersion entries to `src/Directory.Packages.props` (look up current versions on NuGet)
- [x] 1.2 Add `Terp` and `Terp.Akka` PackageReference entries to `src/FunkArr.Core/FunkArr.Core.csproj`
- [x] 1.3 Verify: `dotnet build src/FunkArr.slnx` succeeds with both Servus and Terp side by side

## 2. Namespace renames for helpers

- [x] 2.1 In 10 actor files (DownloadManager, DownloadScheduler, DownloadWorker, HistoryWorker, StatsCollector, RuleSetManager, RuleSetWorker, MovieSearchWorker, TvSearchWorker, SearchManager): replace `using Servus.Akka;` with `using Terp.Akka;`
- [x] 2.2 In DownloadWorker, MovieSearchWorker, TvSearchWorker: replace `using Servus.Resilience;` with `using Terp.Resilience;`
- [x] 2.3 Run `dotnet format src/FunkArr.slnx`
- [x] 2.4 Verify: build, all test projects pass, format check clean, `grep -rn "using Servus" src` only in Configuration/* and Program.cs

## 3. Containers and Program.cs (atomic)

- [x] 3.1 Rewrite all 11 `src/FunkArr/Configuration/*SetupContainer.cs` files per MIGRATION.md step 3 rules: replace Servus base classes/interfaces with Terp ISetup<T>, rename SetupServices→Setup, configuration→context.Configuration, SetupApplication→Setup(WebApplication, SetupContext)
- [x] 3.2 Rewrite `AkkaSetupContainer`: change base from `ActorSystemSetupContainer` to `ActorSystemSetup` (Terp.Akka), `GetActorSystemName()` → `ActorSystemName` property
- [x] 3.3 Rewrite `src/FunkArr/Program.cs`: replace AppBuilder.Create chain with builder.Setups().Add<T>(), builder.Build(), app.Setups().Add<T>(), app.RunAsync(). Remove `using Servus.Application.Startup;` and the type alias
- [x] 3.4 Run `dotnet format src/FunkArr.slnx`
- [x] 3.5 Verify: build, all test projects pass (including IntegrationTests, Api.Tests, Architecture.Tests), format check clean

## 4. Remove own ResolveChildActorExtensions

- [x] 4.1 Delete `src/FunkArr.Core/ResolveChildActorExtensions.cs`
- [x] 4.2 Add `using Terp.Akka;` to `src/FunkArr.Enrichment/EnrichmentManager.cs`
- [x] 4.3 Run `dotnet format src/FunkArr.slnx`
- [x] 4.4 Verify: build, Enrichment.Tests pass, format check clean

## 5. Remove Servus packages

- [x] 5.1 Remove `Servus` and `Servus.Akka` PackageVersion entries from `src/Directory.Packages.props`
- [x] 5.2 Remove `Servus` and `Servus.Akka` PackageReference entries from `src/FunkArr.Core/FunkArr.Core.csproj`
- [x] 5.3 Verify: `grep -rni servus src` finds nothing (ignoring bin/obj), build, all tests, format check clean

## 6. Replace ExternalApiMetrics with TerpHttpMetricsHandler and Gauge<T>

- [x] 6.1 In `EnrichmentSetupContainer.cs` and `SearchSetupContainer.cs`: replace `ExternalApiMetricsHandler` with `TerpHttpMetricsHandler` (`using Terp.Diagnostics;`) for tmdb, tvdb, mediathekviewweb clients
- [x] 6.2 In `TelemetrySetupContainer.cs`: replace `.AddMeter("FunkArr.ExternalApi")` with `.AddMeter("System.Net.Http")`
- [x] 6.3 Delete `src/FunkArr/Configuration/ExternalApiMetrics.cs`
- [x] 6.4 In `FunkArr.Download/Telemetry.cs`: replace 4 ObservableGauge+backing field patterns (queue_size, active, paused, schedule_enabled) with `Gauge<T>` + `.Record()`. Update all call sites from `SetXxx(value)` to `Telemetry.Xxx.Record(value)`
- [x] 6.5 In `FunkArr.RuleSet/Telemetry.cs`: replace 1 ObservableGauge+backing field pattern (active) with `Gauge<T>` + `.Record()`. Update call sites
- [x] 6.6 Optional: replace `.Add(1)` / `.Add(-1)` counter calls with `.Up()` / `.Down()` (`using Terp.Diagnostics;`) across all Telemetry classes (3-tag calls kept as .Add — Up has no 3-tag overload)
- [x] 6.7 Verify: build, all tests pass, format check clean

## 7. Replace stream supervision with ResumeOnError

- [x] 7.1 In DownloadManager, StatsCollector, RuleSetManager, MediathekViewWebManager: replace `.WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))` with `.ResumeOnError()` (add `using Terp.Akka;` where not present)
- [x] 7.2 In FeedCrawlManager: replace `StreamSupervision.LoggingDecider(log)` call with `.ResumeOnError(log)` (N/A — no supervision usage in current code)
- [x] 7.3 Delete `src/FunkArr.Feed/StreamSupervision.cs` (N/A — file did not exist)
- [x] 7.4 Remove `using Akka.Streams.Supervision;` where Deciders was the only reason
- [x] 7.5 Verify: build, all tests pass, format check clean, `grep -rn "Deciders.ResumingDecider\|StreamSupervision" src/` finds nothing

## 8. Documentation updates

- [x] 8.1 Update `openspec/config.yaml`: replace Servus references with Terp equivalents (line 13 and line 29 per MIGRATION.md section 5)
- [x] 8.2 Update `openspec/specs/application-bootstrap/spec.md`: replace Servus AppBuilder references with Terp ISetup startup
- [x] 8.3 Update `openspec/specs/structured-logging/spec.md`: IServiceSetupContainer → ISetup<IServiceCollection>, AppBuilder → builder.Setups()
- [x] 8.4 Update `openspec/specs/logging-setup-container/spec.md`: Servus → Terp references
- [x] 8.5 Update `openspec/specs/setup-container-layout/spec.md`: Servus → Terp references
- [x] 8.6 Update `openspec/specs/backoff-calculation/spec.md`: Servus → Terp references
- [x] 8.7 Update remaining specs per MIGRATION.md section 5 (download-messages, project-structure, solution-infrastructure, ruleset-api, static-file-serving, prometheus-metrics-endpoint)
- [x] 8.8 AGENTS.md and CLAUDE.md Servus→Terp text updates (already done)
- [x] 8.9 Verify: `grep -rni servus AGENTS.md CLAUDE.md openspec/specs openspec/config.yaml` finds nothing
