## Why

FunkArr has zero observability beyond Serilog console logs. No traces, no metrics, no way to see how long searches take, how downloads perform, or where time is spent in the pipeline. `OpenTelemetry.Api` is already pinned in `Directory.Packages.props` but unreferenced.

## What Changes

Three layers of instrumentation, plus local dev observability infrastructure:

### 1. Infrastructure auto-instrumentation

- Register `AddOpenTelemetry()` in a new `TelemetrySetupContainer` with ASP.NET Core and HttpClient auto-instrumentation
- OTLP exporter configured via standard `OTEL_EXPORTER_OTLP_ENDPOINT` env var
- Serilog OTLP log export alongside existing console output
- Zero domain code changes -- immediately shows HTTP request traces and outgoing HTTP calls

### 2. Custom traces (ActivitySource per domain)

Spans at service/runner level where work happens synchronously. No ActivityContext propagation through actor messages -- that would be too invasive.

| Domain | ActivitySource | Spans |
|--------|---------------|-------|
| FunkArr.Search | `FunkArr.Search` | `search.tv`, `search.movie` (around the full worker flow) |
| FunkArr.Download | `FunkArr.Download` | `download.remux` (Remuxer.RunAsync), `download.subtitle` (SubtitlePreparer), `download.ffmpeg` (FfmpegRunner -- already has Stopwatch) |
| FunkArr.Scoring | `FunkArr.Scoring` | `scoring.evaluate` (ScoringEngine per-ruleset evaluation) |
| FunkArr.Enrichment | `FunkArr.Enrichment` | `enrichment.tmdb`, `enrichment.tvdb` (per-lookup) |

### 3. Custom metrics (Meter per domain)

| Metric | Type | Source |
|--------|------|--------|
| `funkarr.search.duration` | Histogram | Search workers |
| `funkarr.search.results` | Histogram | Search workers (result count) |
| `funkarr.download.duration` | Histogram | FfmpegRunner |
| `funkarr.download.completed` | Counter | DownloadHistoryManager |
| `funkarr.download.failed` | Counter | DownloadHistoryManager |
| `funkarr.download.active` | UpDownCounter | DownloadManager |
| `funkarr.scoring.duration` | Histogram | ScoringEngine |
| `funkarr.enrichment.cache_hits` | Counter | TmdbClient/TvdbClient |
| `funkarr.enrichment.cache_misses` | Counter | TmdbClient/TvdbClient |

### 4. Aspire Dashboard in docker-compose.dev.yml

Standalone Aspire Dashboard container (no Aspire SDK) receives OTLP and shows traces, metrics, and logs in a web UI. No production dependency.

## Capabilities

### New Capabilities

- `telemetry-setup`: TelemetrySetupContainer registering OpenTelemetry with tracing, metrics, OTLP export
- `search-telemetry`: ActivitySource + Meter in FunkArr.Search
- `download-telemetry`: ActivitySource + Meter in FunkArr.Download
- `scoring-telemetry`: ActivitySource + Meter in FunkArr.Scoring
- `enrichment-telemetry`: ActivitySource + Meter in FunkArr.Enrichment
- `aspire-dashboard`: Standalone Aspire Dashboard in docker-compose.dev.yml

### Modified Capabilities

- `logging-setup`: Serilog gains OTLP exporter alongside console

## Impact

- **FunkArr (Host)**: New `TelemetrySetupContainer`, new OTel SDK packages
- **FunkArr.Search**: `OpenTelemetry.Api` package ref, ActivitySource + Meter in workers
- **FunkArr.Download**: `OpenTelemetry.Api` package ref, ActivitySource + Meter in FfmpegRunner, SubtitlePreparer, Remuxer
- **FunkArr.Scoring**: `OpenTelemetry.Api` package ref, ActivitySource + Meter in ScoringEngine
- **FunkArr.Enrichment**: `OpenTelemetry.Api` package ref, ActivitySource + Meter in TmdbClient, TvdbClient
- **docker-compose.dev.yml**: Aspire Dashboard service
- No message changes, no actor interface changes
