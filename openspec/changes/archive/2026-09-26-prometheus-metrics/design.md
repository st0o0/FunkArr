## Context

FunkArr currently pushes telemetry (traces + metrics) via OTLP to an Aspire Dashboard container. This requires an extra container, couples the app to a specific dashboard, and provides distributed tracing that adds no value for a single-service application. The *arr ecosystem convention is to expose a Prometheus `/metrics` endpoint that users scrape with their own monitoring stack.

Current state:
- 4 custom Meters (Search, Download, Scoring, Enrichment) with 8 instruments
- 3 ActivitySources with 6 trace spans
- OTLP exporter pushing to Aspire Dashboard on port 18889
- `TelemetrySetupContainer` wires both pipelines
- Each domain project references `OpenTelemetry.Api` for Meter + ActivitySource

## Goals / Non-Goals

**Goals:**
- Expose all metrics via Prometheus scrape endpoint at `/metrics`
- Provide metrics that give real operational insight (queue depth, hit rates, API health)
- Include standard ASP.NET Core and .NET process metrics
- Remove all tracing infrastructure
- Remove Aspire Dashboard dependency

**Non-Goals:**
- Providing a built-in dashboard or UI for metrics
- Supporting push-based export (OTLP, Graphite, etc.)
- Custom Prometheus metric prefixing for ASP.NET/process metrics (standard names, `job` label differentiates)

## Decisions

### 1. Prometheus exporter via `OpenTelemetry.Exporter.Prometheus.AspNetCore`

Use the OpenTelemetry Prometheus ASP.NET Core exporter which maps `app.MapPrometheusScrapingEndpoint("/metrics")`. This keeps the OTel Meter API as the instrumentation layer (no direct Prometheus client dependency in domain projects) while exporting in Prometheus text format.

**Alternative**: `prometheus-net` library directly. Rejected because the codebase already uses OTel Meters and switching instrumentation APIs adds unnecessary churn.

### 2. Keep `OpenTelemetry.Api` in domain projects for Meters only

Domain projects (Search, Download, Scoring, Enrichment) keep `OpenTelemetry.Api` for `Meter` and instrument definitions. The `ActivitySource` definitions and all tracing callsites are removed. The `Telemetry.cs` files in each domain are simplified to Meter-only.

**Alternative**: Move all metric definitions to the host project. Rejected because metrics are domain-specific and belong with the code that records them.

### 3. Redesigned metric set

All custom metrics use `funkarr_` prefix. The instrument set is redesigned around operational questions:

**Search domain:**
- `funkarr_search_requests_total` (Counter, tags: `source`=sonarr/radarr/manual) - volume
- `funkarr_search_matches_total` (Counter) - successful matches
- `funkarr_search_no_match_total` (Counter) - empty results

**Download domain:**
- `funkarr_download_queue_size` (Gauge) - current queue depth
- `funkarr_download_active` (Gauge) - currently downloading
- `funkarr_download_completed_total` (Counter) - lifetime completions
- `funkarr_download_failed_total` (Counter, tags: `reason`) - failures with cause
- `funkarr_download_bytes_total` (Counter) - total bytes downloaded
- `funkarr_download_duration_seconds` (Histogram) - time per download

**Scoring domain:**
- `funkarr_scoring_accepted_total` (Counter) - results passing rulesets
- `funkarr_scoring_rejected_total` (Counter) - results rejected by rulesets

**Enrichment domain:**
- `funkarr_enrichment_requests_total` (Counter, tags: `api`=tmdb/tvdb, `status`=hit/miss) - cache effectiveness

**External API health (new, in host or Search/Enrichment):**
- `funkarr_external_api_requests_total` (Counter, tags: `api`, `status_code`) - request counts
- `funkarr_external_api_duration_seconds` (Histogram, tags: `api`) - latency per external API

### 4. ASP.NET + Process metrics via standard instrumentation

`AddAspNetCoreInstrumentation()` for HTTP server metrics and `AddProcessInstrumentation()` / `AddRuntimeInstrumentation()` for GC, CPU, threads. These use OTel semantic convention names and are distinguished from other .NET services via Prometheus `job` label in the scrape config.

### 5. External API metrics replace tracing

The current HTTP client tracing (auto-instrumented spans on TMDB/TVDB/MediathekViewWeb calls) is replaced by explicit counter + histogram metrics tagged by API name. This provides the same insight (latency, error rates) in aggregated form without the tracing overhead.

## Risks / Trade-offs

- **Loss of per-request trace visibility** - Individual slow requests are no longer traceable through spans. Mitigation: structured logging already captures per-request details; histogram percentiles surface latency outliers.
- **Prometheus cardinality** - Tags like `reason` on download failures or `status_code` on external API calls can grow. Mitigation: use bounded tag values (enum-style reasons, status code classes like 2xx/4xx/5xx).
- **Breaking change for existing Aspire Dashboard users** - Anyone who set up Aspire Dashboard loses their telemetry view. Mitigation: version 0.x, breaking changes are expected; document `/metrics` endpoint in release notes.
