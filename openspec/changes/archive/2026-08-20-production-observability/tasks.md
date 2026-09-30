## 1. NuGet Dependencies

- [x] 1.1 Add `prometheus-net.AspNetCore` to `Directory.Packages.props` and `FunkArr.csproj`
- [x] 1.2 Add `Akka.HealthCheck.Hosting.Web` to `Directory.Packages.props` and `FunkArr.csproj`
- [x] 1.3 Run `dotnet restore` and verify both packages resolve without conflicts

## 2. Metrics Infrastructure

- [x] 2.1 Create `Diagnostics/FunkArrMetrics.cs` — singleton with `Meter("FunkArr")`, private constructor, static `Instance` property
- [x] 2.2 Create `Diagnostics/SearchMetricsExtensions.cs` — `AddSearchTotal` (Counter), `AddSearchDuration` (Histogram), `AddCacheHitTotal` (Counter)
- [x] 2.3 Create `Diagnostics/DownloadMetricsExtensions.cs` — `AddDownloadTotal` (Counter), `AddDownloadDuration` (Histogram), `AddQueueDepth` (Gauge)
- [x] 2.4 Create `Diagnostics/MuxingMetricsExtensions.cs` — `AddMuxDuration` (Histogram)
- [x] 2.5 Create `Diagnostics/ApiClientMetricsExtensions.cs` — `AddApiCallTotal` (Counter), `AddApiCallDuration` (Histogram)

## 3. Prometheus Endpoint Wiring

- [x] 3.1 Add `Metrics.ConfigureMeterAdapter` with filter `instrument.Meter.Name == "FunkArr"` in `FunkArrServiceSetup`
- [x] 3.2 Add `app.UseHttpMetrics()` and `app.MapMetrics()` in `FunkArrApplicationSetup`

## 4. Instrument Search Subsystem

- [x] 4.1 Add search metric fields to `SearchActor` — create instruments in constructor, record on cache hits and `SearchCompleted` handling
- [x] 4.2 Verify search metrics appear on `/metrics` endpoint

## 5. Instrument Download Subsystem

- [x] 5.1 Add download metric fields to `DownloadQueueActor` — create instruments in constructor, record on enqueue/complete/error events and queue depth changes
- [x] 5.2 Verify download metrics appear on `/metrics` endpoint

## 6. Instrument Muxing Subsystem

- [x] 6.1 Add mux duration metric to `MuxingService` — create instrument, record on mux completion
- [x] 6.2 Verify muxing metrics appear on `/metrics` endpoint

## 7. Instrument API Clients

- [x] 7.1 Add API call metrics to `MediathekClient` — create instruments, record call count and duration
- [x] 7.2 Add API call metrics to `TvdbClient` — create instruments, record call count and duration
- [x] 7.3 Add API call metrics to `TmdbClient` — create instruments, record call count and duration
- [x] 7.4 Add API call metrics to `GitHubReleaseClient` — create instruments, record call count and duration

## 8. Akka Health Checks

- [x] 8.1 Add `services.WithAkkaHealthCheck(HealthCheckType.Default | HealthCheckType.Persistence)` in `FunkArrServiceSetup`
- [x] 8.2 Add `builder.WithWebHealthCheck(sp)` in `FunkArrActorSystemSetup.BuildSystem()` — pass the `IServiceProvider` parameter
- [x] 8.3 Add `app.MapAkkaHealthCheckRoutes()` in `FunkArrApplicationSetup`

## 9. Verification

- [x] 9.1 Run `dotnet build FunkArr.slnx` and verify clean build
- [x] 9.2 Run all tests and verify no regressions
- [x] 9.3 Start the service and verify `/metrics` returns Prometheus output with `funkarr_*` metrics
- [x] 9.4 Start the service and verify `/healthz/akka/live` and `/healthz/akka/ready` return 200
- [x] 9.5 Verify existing `/healthz` and `/alive` endpoints still work
