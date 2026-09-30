## Context

FunkArr currently has no runtime metrics and only a single health check (FFmpeg binary availability). The reference project Njord has full Prometheus instrumentation (16 instruments across 6 extension files) and custom ASP.NET Core health checks. FunkArr should adopt the same metrics pattern and add Akka.Hosting health checks for actor system and persistence monitoring.

Current startup chain: `FunkArrServiceSetup` → `FunkArrActorSystemSetup` → `FunkArrApplicationSetup`. All three files need modifications. The existing `/healthz` and `/alive` endpoints must remain intact.

## Goals / Non-Goals

**Goals:**
- Expose Prometheus-compatible `/metrics` endpoint with FunkArr-specific instruments
- Expose Akka health check endpoints (`/healthz/akka/*`) for node liveness, readiness, and persistence probes
- Follow Njord's proven singleton + extension method pattern for metrics
- Keep existing health infrastructure unchanged

**Non-Goals:**
- Grafana dashboards or alerting rules (downstream concern)
- OpenTelemetry tracing or distributed tracing integration
- Custom health checks beyond what Akka.HealthCheck provides (e.g., SearchActor readiness)
- Refactoring API clients to structured error types (separate change)

## Decisions

### D1: Metrics singleton pattern (not DI-registered)

`FunkArrMetrics` will be a singleton accessed via `FunkArrMetrics.Instance`, identical to Njord's `NjordMetrics`. The `Meter` is not registered in DI.

**Rationale**: Metrics instruments need to be accessible from actors, which don't participate in request-scoped DI. A static singleton avoids passing metrics through constructor injection everywhere. This pattern is proven in Njord and aligns with how `System.Diagnostics.Metrics` is designed to be used.

**Alternative considered**: DI-registered singleton — rejected because actors resolve dependencies at construction time via `IDependencyResolver`, and adding metrics to every actor's constructor is unnecessary ceremony for what is essentially a global concern.

### D2: Extension methods per subsystem

Each subsystem gets its own `*MetricsExtensions.cs` file in `Diagnostics/`:
- `SearchMetricsExtensions.cs`
- `DownloadMetricsExtensions.cs`
- `MuxingMetricsExtensions.cs`
- `ApiClientMetricsExtensions.cs`

Each extension method creates and returns one instrument (Counter, Histogram, or Gauge).

**Rationale**: Matches Njord's pattern. Keeps metric definitions co-located by domain concern. Extension methods are discoverable via IntelliSense on `FunkArrMetrics`.

### D3: prometheus-net.AspNetCore for Prometheus exposition

Use `prometheus-net.AspNetCore` (same as Njord) with `Metrics.ConfigureMeterAdapter` filtered to the `"FunkArr"` meter name.

**Rationale**: Single NuGet package provides both the meter-to-Prometheus bridge and the `/metrics` endpoint middleware. Proven in Njord. The meter filter ensures only FunkArr instruments are exported (not internal .NET or Akka metrics).

### D4: Built-in Akka.Hosting health checks (not deprecated Akka.HealthCheck package)

Use the health check APIs built into `Akka.Hosting` 1.5.70+, which replaced the deprecated `Akka.HealthCheck.Hosting.Web` package. This provides:
- `WithActorSystemLivenessCheck()` — ActorSystem liveness probe
- `WithSqlPersistence(..., journalBuilder: j => j.WithHealthCheck(), snapshotBuilder: s => s.WithHealthCheck())` — persistence probes for journal and snapshot store

These register directly with `Microsoft.Extensions.Diagnostics.HealthChecks` and appear in the existing `/healthz` endpoint. No separate routes needed.

**Rationale**: The deprecated `Akka.HealthCheck.Hosting.Web` package pulls in old `Akka.Cluster` 1.5.37 and `Akka.Remote` 1.5.37 with known vulnerabilities. The built-in API in `Akka.Hosting` 1.5.70 provides the same functionality without extra dependencies.

### D5: Instrument recording locations

Instruments will be created (via extension methods) and stored as fields in the component that records them:
- **SearchActor**: search counters, cache hit counter, duration histogram
- **DownloadQueueActor**: download counter, duration histogram, queue depth gauge
- **MuxingService**: mux duration histogram
- **MediathekClient, TvdbClient, TmdbClient, GitHubReleaseClient**: API call counter and duration histogram

**Rationale**: Each component owns its metrics. Instruments are created once in the constructor and recorded inline at the call site. No centralized "metrics service" needed.

### D6: Metric naming convention

All instruments use the prefix `funkarr_` with snake_case naming. Units are expressed in the metric name suffix (`_seconds`, `_total`). This follows Prometheus naming conventions.

## Risks / Trade-offs

**[Risk] prometheus-net version compatibility** → Check NuGet for the latest `prometheus-net.AspNetCore` version compatible with .NET 10. Njord uses 8.2.1; verify this works.

**[Risk] Akka.HealthCheck 1.5.37 with Akka 1.5.70** → The health check package versions independently from Akka.Hosting. 1.5.37 targets Akka ≥1.5.37 as a minimum; should be forward-compatible. Verify at `dotnet restore` time.

**[Trade-off] No custom health checks** → We're not adding SearchActor readiness or MediathekViewWeb reachability checks. These could be added later as custom `IHealthCheck` implementations alongside the Akka probes.

**[Trade-off] Metrics add constructor parameters to actors** → Each instrumented actor/service gains 2-4 field assignments in its constructor. This is minimal overhead but increases the constructor surface slightly.
