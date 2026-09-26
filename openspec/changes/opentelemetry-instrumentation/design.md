## Approach

Three layers built bottom-up: infrastructure auto-instrumentation first, then custom traces, then metrics. Each layer is independently useful.

## Layer 1: Infrastructure

New `TelemetrySetupContainer` in FunkArr host. Registers `AddOpenTelemetry()` with:
- Tracing: ASP.NET Core + HttpClient auto-instrumentation + domain ActivitySources
- Metrics: ASP.NET Core + domain Meters
- OTLP exporter (gRPC) configured via standard `OTEL_EXPORTER_OTLP_ENDPOINT` env var
- When no endpoint is configured, OTLP export is a no-op (dev without dashboard still works)

Serilog stays as-is for console output. OTel handles trace/metric export separately. No Serilog OTLP bridge needed in this change.

New packages in Directory.Packages.props + FunkArr.csproj:
- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`

Domain projects add only `OpenTelemetry.Api` (already pinned).

## Layer 2: Custom Traces

One static `ActivitySource` per domain project, defined as a static field on a `Telemetry` class:

```
// In each domain project:
internal static class Telemetry
{
    internal static readonly ActivitySource Source = new("FunkArr.<Domain>");
}
```

Span placement (synchronous service-level, NOT across actor messages):

**FunkArr.Download:**
- `Remuxer.RunAsync` -- parent span `download.remux` with download ID tag
- `SubtitlePreparer.PrepareAsync` -- child span `download.subtitle`
- `FfmpegRunner.RunAsync` -- child span `download.ffmpeg` (replaces existing Stopwatch)

**FunkArr.Scoring:**
- `ScoringEngine.Score` -- span `scoring.evaluate` with ruleset ID + candidate count tags

**FunkArr.Enrichment:**
- `TmdbClient` lookup method -- span `enrichment.tmdb` with cache hit/miss tag
- `TvdbClient` lookup method -- span `enrichment.tvdb` with cache hit/miss tag

**FunkArr.Search:**
- Search workers are Become-based state machines where spans can't cross actor boundaries. Skip custom traces here -- ASP.NET auto-instrumentation already captures the HTTP request duration. Add metrics instead.

## Layer 3: Custom Metrics

One static `Meter` per domain project alongside the `ActivitySource`:

```
internal static class Telemetry
{
    internal static readonly ActivitySource Source = new("FunkArr.<Domain>");
    internal static readonly Meter Meter = new("FunkArr.<Domain>");
}
```

**FunkArr.Search:**
- `search.duration` Histogram (seconds) -- recorded when search worker replies
- `search.results` Histogram (count) -- number of results returned

**FunkArr.Download:**
- `download.completed` Counter -- incremented by DownloadHistoryManager on success
- `download.failed` Counter -- incremented by DownloadHistoryManager on failure

**FunkArr.Scoring:**
- `scoring.duration` Histogram (seconds) -- recorded in ScoringEngine.Score

**FunkArr.Enrichment:**
- `enrichment.cache_hits` Counter -- with `source` tag (tmdb/tvdb)
- `enrichment.cache_misses` Counter -- with `source` tag (tmdb/tvdb)

## Aspire Dashboard

Add to `docker-compose.dev.yml` as standalone container:
- Image: `mcr.microsoft.com/dotnet/aspire-dashboard`
- Ports: 18888 (UI), 4317 mapped to 18889 (OTLP gRPC)
- FunkArr gets `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889`
- Anonymous access enabled for local dev
