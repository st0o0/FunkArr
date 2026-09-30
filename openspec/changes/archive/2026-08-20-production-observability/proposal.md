## Why

FunkArr has zero runtime observability. There are no metrics to track search performance, download throughput, API client health, or cache effectiveness. The only health check is FFmpeg binary availability — nothing monitors actor system health or persistence connectivity. Without observability, production issues are invisible until users report them. Njord (same author, same patterns) is production-ready with full Prometheus metrics and structured health checks; FunkArr should match that standard.

## What Changes

- Add `FunkArrMetrics` singleton with a `System.Diagnostics.Metrics.Meter` named `"FunkArr"`, following Njord's proven pattern
- Add per-subsystem metric extension methods: Search, Download, Muxing, API clients
- Add `prometheus-net.AspNetCore` NuGet package for Prometheus scrape endpoint
- Wire `Metrics.ConfigureMeterAdapter` (filtered to `"FunkArr"` meter), `UseHttpMetrics()`, and `MapMetrics()` for `/metrics` endpoint
- Add `Akka.HealthCheck.Hosting.Web` NuGet package for actor system and persistence health probes
- Wire Akka health check probes: node liveness, node readiness, persistence liveness (journal + snapshot store)
- Map Akka health check routes (`/healthz/akka/*`) alongside existing `/healthz` and `/alive` endpoints
- Keep existing `FfmpegHealthCheck` unchanged

## Capabilities

### New Capabilities
- `prometheus-metrics`: Prometheus-compatible metrics instrumentation via `System.Diagnostics.Metrics` and `prometheus-net`, covering search, download, muxing, and API client subsystems
- `akka-health-checks`: Akka.NET actor system and persistence health probes via `Akka.HealthCheck.Hosting.Web`, exposing liveness and readiness endpoints

### Modified Capabilities

## Impact

- **Dependencies**: Two new NuGet packages (`prometheus-net.AspNetCore`, `Akka.HealthCheck.Hosting.Web`) added to `Directory.Packages.props` and `FunkArr.csproj`
- **Startup**: All three setup containers modified (ServiceSetup for DI/metrics config, ActorSystemSetup for Akka health check wiring, ApplicationSetup for endpoint mapping)
- **New files**: `Diagnostics/FunkArrMetrics.cs` + per-subsystem `*MetricsExtensions.cs` files
- **Modified actors/services**: SearchActor, DownloadQueueActor, MuxingActor, and API clients instrumented with metric recording calls
- **API surface**: New `/metrics` endpoint (Prometheus scrape), new `/healthz/akka/*` endpoints (Akka probes). Existing `/healthz` and `/alive` unchanged
- **Docker**: Prometheus scrape port/path may need documenting in `docker-compose.example.yml`
