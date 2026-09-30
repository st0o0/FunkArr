## 1. Package changes

- [x] 1.1 Add `OpenTelemetry.Exporter.Prometheus.AspNetCore` and `OpenTelemetry.Instrumentation.Process` / `OpenTelemetry.Instrumentation.Runtime` to `Directory.Packages.props` and `FunkArr.csproj`
- [x] 1.2 Remove `OpenTelemetry.Exporter.OpenTelemetryProtocol` and `OpenTelemetry.Instrumentation.Http` from `Directory.Packages.props` and `FunkArr.csproj`

## 2. Remove tracing

- [x] 2.1 Remove all `ActivitySource` definitions from `Telemetry.cs` files in Download, Scoring, Enrichment
- [x] 2.2 Remove all tracing callsites (`StartActivity`, `SetTag`) from `FfmpegRunner.cs`, `SubtitlePreparer.cs`, `Remuxer.cs`, `ScoringEngine.cs`, `TvdbClient.cs`, `TmdbClient.cs`
- [x] 2.3 Remove tracing pipeline from `TelemetrySetupContainer.cs` (`.WithTracing()` block and ActivitySource registrations)

## 3. Redesign metrics

- [x] 3.1 Rewrite `FunkArr.Search/Telemetry.cs`: replace `search.duration` + `search.results` with `funkarr_search_requests_total`, `funkarr_search_matches_total`, `funkarr_search_no_match_total`
- [x] 3.2 Rewrite `FunkArr.Download/Telemetry.cs`: replace existing instruments with `funkarr_download_queue_size`, `funkarr_download_active`, `funkarr_download_completed_total`, `funkarr_download_failed_total`, `funkarr_download_bytes_total`, `funkarr_download_duration_seconds`
- [x] 3.3 Rewrite `FunkArr.Scoring/Telemetry.cs`: replace `scoring.duration` with `funkarr_scoring_accepted_total`, `funkarr_scoring_rejected_total`
- [x] 3.4 Rewrite `FunkArr.Enrichment/Telemetry.cs`: replace `cache_hits`/`cache_misses` with `funkarr_enrichment_requests_total` (tags: api, status)
- [x] 3.5 Add external API health metrics: `funkarr_external_api_requests_total` and `funkarr_external_api_duration_seconds` (tags: api, status_code)

## 4. Update metric recording callsites

- [x] 4.1 Update `TvSearchWorker.cs` and `MovieSearchWorker.cs` to record new search metrics (requests, matches, no-match)
- [x] 4.2 Update `DownloadHistoryManager.cs` to record new download completion/failure metrics with reason tags
- [x] 4.3 Update `FfmpegRunner.cs` to record download duration and bytes
- [x] 4.4 Add queue size and active download gauge updates in `DownloadManager` (or wherever queue state lives)
- [x] 4.5 Update `ScoringEngine.cs` to record accepted/rejected counts instead of duration
- [x] 4.6 Update `TvdbClient.cs` and `TmdbClient.cs` to record enrichment requests with api/status tags
- [x] 4.7 Add external API metrics recording via `DelegatingHandler` or directly in HTTP client callsites

## 5. Switch exporter pipeline

- [x] 5.1 Rewrite `TelemetrySetupContainer.cs`: remove tracing, switch metrics to Prometheus exporter, add ASP.NET Core + Process/Runtime instrumentation, register all Meters
- [x] 5.2 Add `app.MapPrometheusScrapingEndpoint("/metrics")` to the application pipeline

## 6. Remove Aspire Dashboard

- [x] 6.1 Remove `aspire-dashboard` service from `docker-compose.dev.yml`
- [x] 6.2 Remove `OTEL_EXPORTER_OTLP_ENDPOINT` env var from FunkArr service in `docker-compose.dev.yml`

## 7. Verify

- [x] 7.1 Build solution, run all tests, verify no tracing references remain
- [x] 7.2 Docker compose up, curl `/metrics`, verify custom + ASP.NET + process metrics are present
