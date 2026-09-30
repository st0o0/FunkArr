## Why

The current observability stack uses push-based OTLP export to an Aspire Dashboard container. This adds infrastructure overhead (extra container), couples FunkArr to a specific dashboard, and provides tracing that has minimal value for a single-service application. Users in the *arr ecosystem expect a standard Prometheus `/metrics` endpoint they can scrape with their own monitoring stack.

## What Changes

- **BREAKING**: Remove OTLP push export and Aspire Dashboard container
- **BREAKING**: Remove all distributed tracing (ActivitySource instrumentation)
- Add Prometheus scrape endpoint at `/metrics`
- Redesign custom metrics for operational value (queue depth, search hit rates, external API health, download throughput)
- Include ASP.NET Core and process runtime metrics (standard names, distinguished by Prometheus `job` label)
- Remove `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.Http` (tracing-only) packages
- Add `OpenTelemetry.Exporter.Prometheus.AspNetCore` package

## Capabilities

### New Capabilities

- `prometheus-metrics-endpoint`: Prometheus-compatible `/metrics` scrape endpoint exposing custom application metrics, ASP.NET Core HTTP metrics, and .NET process runtime metrics

### Modified Capabilities

_(none - this replaces the telemetry implementation without changing spec-level behavior of other capabilities)_

## Impact

- **Packages**: Remove `OpenTelemetry.Exporter.OpenTelemetryProtocol`, add `OpenTelemetry.Exporter.Prometheus.AspNetCore`, add `OpenTelemetry.Instrumentation.Process` or `OpenTelemetry.Instrumentation.Runtime`; remove `OpenTelemetry.Api` from domain projects that only used it for `ActivitySource`
- **Code**: Rewrite `TelemetrySetupContainer` (switch exporter, remove tracing pipeline); delete all `Telemetry.cs` files containing `ActivitySource` definitions; delete tracing callsites in `FfmpegRunner`, `SubtitlePreparer`, `Remuxer`, `ScoringEngine`, `TvdbClient`, `TmdbClient`; redesign `Meter`/instrument definitions across Search, Download, Scoring, Enrichment
- **Infrastructure**: Remove `aspire-dashboard` service and `OTEL_EXPORTER_OTLP_ENDPOINT` env var from `docker-compose.dev.yml`
- **Config**: Document `/metrics` endpoint availability for users
