## Why

Servus and Servus.Akka are being replaced by their successor libraries Terp and
Terp.Akka. Terp has a cleaner setup model (`ISetup<T>` with pattern-matching
dispatch, trim/AOT-safe), better diagnostics helpers, and is actively maintained
under the same author. The migration has zero data impact — no persisted types or
HOCON config reference Servus.

## What Changes

- Replace `Servus` 0.35.0 and `Servus.Akka` 0.3.15 NuGet packages with `Terp`
  and `Terp.Akka`.
- Rename `using Servus.*` to `using Terp.*` across all actor files and setup
  containers.
- Rewrite all setup containers from Servus's `IServiceSetupContainer` /
  `ApplicationSetupContainer<WebApplication>` / `ActorSystemSetupContainer` to
  Terp's `ISetup<IServiceCollection>` / `ISetup<WebApplication>` /
  `ActorSystemSetup`.
- Rewrite `Program.cs` from `AppBuilder.Create(...).WithSetup<T>().Build()` /
  `runner.RunAsync()` to `builder.Setups().Add<T>()` / `app.Setups().Add<T>()` /
  `app.RunAsync()`.
- Delete the own `ResolveChildActorExtensions` in Core (Terp.Akka provides it).
- Replace `ExternalApiMetricsHandler` with `TerpHttpMetricsHandler`
  (`Terp.Diagnostics`). **BREAKING** metric names change from
  `funkarr_external_api_*` to `http_client_request_duration_seconds` with `api`
  tag.
- Replace `ObservableGauge<T>` + backing field pattern with `Gauge<T>.Record()`
  (.NET 9+) in Download and RuleSet telemetry.
- Replace `.WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))`
  with `.ResumeOnError()` (Terp.Akka) in 4 stream sites, and the custom
  `StreamSupervision.LoggingDecider` with `.ResumeOnError(log)` in FeedCrawlManager.
- Optional: replace `.Add(1)` counter calls with `Terp.Diagnostics.CounterExtensions`
  `.Up()` / `.Down()`.
- Update openspec/specs and openspec/config.yaml to reference Terp instead of Servus.

## Capabilities

### New Capabilities

_(none — this is a library swap, not a feature addition)_

### Modified Capabilities

- `application-bootstrap`: Startup model changes from Servus AppBuilder to Terp
  ISetup<T> with builder.Setups().Add<T>() / app.Setups().Add<T>().
- `prometheus-metrics-endpoint`: External API metrics change from custom
  FunkArr.ExternalApi meter to System.Net.Http via TerpHttpMetricsHandler.

## Impact

- **NuGet packages**: Remove Servus + Servus.Akka, add Terp + Terp.Akka in
  Directory.Packages.props and FunkArr.Core.csproj.
- **All setup containers** (11 files in src/FunkArr/Configuration/): base
  class/interface and method signatures change.
- **Program.cs**: Complete rewrite of startup chain.
- **10 actor files**: `using` namespace changes (Servus.Akka → Terp.Akka,
  Servus.Resilience → Terp.Resilience).
- **5 stream sites**: Supervision strategy helper replacement.
- **Metrics dashboards/alerts**: External API metric names change (step 6).
  Download/RuleSet gauge backing fields replaced with Gauge<T> (no metric name
  change).
- **Deleted files**: ResolveChildActorExtensions.cs (Core),
  ExternalApiMetrics.cs (Configuration), StreamSupervision.cs (Feed).
- **openspec specs + config.yaml**: Text references updated.
- **No data migration**: No persisted types, HOCON, or wire formats change.
