# Observability

FunkArr exports traces and metrics via [OpenTelemetry](https://opentelemetry.io/) (OTLP). This lets you monitor download pipelines, scoring runs, and enrichment requests in any OTLP-compatible backend.

## What is instrumented?

### Tracing

| Source | Description |
|--------|------------|
| ASP.NET Core | Incoming HTTP requests (all API endpoints) |
| HttpClient | Outgoing HTTP requests (MVW, TMDB, TVDB, etc.) |
| `FunkArr.Download` | Download pipeline: queue, fetch, FFmpeg remux |
| `FunkArr.Scoring` | Scoring runs: ruleset matching, score calculation |
| `FunkArr.Enrichment` | Metadata enrichment: TMDB/TVDB lookups |

### Metrics

| Meter | Description |
|-------|------------|
| ASP.NET Core | Request rate, latency, error rate |
| `FunkArr.Search` | Search queries, results, duration |
| `FunkArr.Download` | Downloads, bytes, speed |
| `FunkArr.Scoring` | Scoring runs, matches, duration |
| `FunkArr.Enrichment` | TMDB/TVDB lookups, cache hits |

## Configuration

FunkArr uses the standard OTLP exporter. Configuration is via the official OpenTelemetry environment variable:

| Variable | Default | Description |
|----------|---------|------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | _(empty)_ | OTLP receiver URL (e.g. `http://aspire-dashboard:18889`). Without a value, export is disabled. |

::: tip
Without `OTEL_EXPORTER_OTLP_ENDPOINT` set, FunkArr starts normally but does not export telemetry data. There is no overhead when no receiver is configured.
:::

## Aspire Dashboard

The easiest way to view traces and metrics is the [.NET Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone). It runs as a single container alongside FunkArr.

### Docker Compose Setup

```yaml
services:
  funkarr:
    image: ghcr.io/st0o0/funkarr:latest
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
    # ... rest of FunkArr configuration

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ports:
      - "18888:18888"
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
```

After startup the dashboard is available at `http://localhost:18888`.

### What you see there

- **Traces** - Distributed traces for every request flow: from search query through scoring and enrichment to download
- **Metrics** - Live dashboards for request rates, download speeds, and scoring statistics
- **Structured Logs** - All Serilog log entries as structured data (when an OTLP log exporter is configured)

## Other backends

Any OTLP-compatible backend works - point `OTEL_EXPORTER_OTLP_ENDPOINT` to the respective receiver:

- **Grafana Tempo/Mimir** - `http://tempo:4317`
- **Jaeger** - `http://jaeger:4317`
- **Seq** - `http://seq:5341/ingest/otlp`

Other OpenTelemetry environment variables (e.g. `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME`) are also supported. See the [OpenTelemetry SDK documentation](https://opentelemetry.io/docs/languages/net/configuration/) for all options.
