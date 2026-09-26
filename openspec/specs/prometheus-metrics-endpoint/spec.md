## ADDED Requirements

### Requirement: Prometheus scrape endpoint

The system SHALL expose a Prometheus-compatible metrics endpoint at `/metrics` that returns metrics in Prometheus text exposition format.

#### Scenario: Prometheus scrapes metrics

- **WHEN** a GET request is made to `/metrics`
- **THEN** the response has Content-Type `text/plain; version=0.0.4; charset=utf-8` and contains metric families in Prometheus text format

#### Scenario: Metrics endpoint is always available

- **WHEN** the application is running and healthy
- **THEN** `/metrics` returns HTTP 200 without authentication

### Requirement: Search metrics

The system SHALL expose search metrics that indicate search volume and effectiveness.

#### Scenario: Search request recorded

- **WHEN** a search operation completes (with or without results)
- **THEN** `funkarr_search_requests_total` is incremented with a `source` tag indicating the origin (sonarr, radarr, manual)

#### Scenario: Search match recorded

- **WHEN** a search operation returns at least one accepted result
- **THEN** `funkarr_search_matches_total` is incremented

#### Scenario: Search miss recorded

- **WHEN** a search operation returns zero accepted results
- **THEN** `funkarr_search_no_match_total` is incremented

### Requirement: Download metrics

The system SHALL expose download metrics that indicate queue state, throughput, and reliability.

#### Scenario: Queue depth observable

- **WHEN** `/metrics` is scraped
- **THEN** `funkarr_download_queue_size` reflects the current number of items in the download queue

#### Scenario: Active downloads observable

- **WHEN** `/metrics` is scraped
- **THEN** `funkarr_download_active` reflects the current number of downloads in progress

#### Scenario: Download completion recorded

- **WHEN** a download completes successfully
- **THEN** `funkarr_download_completed_total` is incremented, `funkarr_download_bytes_total` is incremented by the file size in bytes, and `funkarr_download_duration_seconds` records the elapsed time

#### Scenario: Download failure recorded

- **WHEN** a download fails
- **THEN** `funkarr_download_failed_total` is incremented with a `reason` tag indicating the failure cause (network, ffmpeg, disk, other)

### Requirement: Scoring metrics

The system SHALL expose scoring metrics that indicate ruleset effectiveness.

#### Scenario: Accepted result recorded

- **WHEN** a search result passes scoring evaluation
- **THEN** `funkarr_scoring_accepted_total` is incremented

#### Scenario: Rejected result recorded

- **WHEN** a search result is rejected by scoring evaluation
- **THEN** `funkarr_scoring_rejected_total` is incremented

### Requirement: Enrichment metrics

The system SHALL expose enrichment metrics that indicate external metadata lookup performance.

#### Scenario: Enrichment request recorded

- **WHEN** a metadata enrichment request is made to TMDB or TVDB
- **THEN** `funkarr_enrichment_requests_total` is incremented with tags `api` (tmdb/tvdb) and `status` (hit/miss)

### Requirement: External API health metrics

The system SHALL expose metrics for external API calls to enable monitoring of upstream service health.

#### Scenario: External API call recorded

- **WHEN** an HTTP request to an external API (MediathekViewWeb, TMDB, TVDB) completes
- **THEN** `funkarr_external_api_requests_total` is incremented with tags `api` and `status_code` (grouped as 2xx/4xx/5xx)

#### Scenario: External API latency recorded

- **WHEN** an HTTP request to an external API completes
- **THEN** `funkarr_external_api_duration_seconds` records the elapsed time with an `api` tag

### Requirement: Standard runtime metrics

The system SHALL expose ASP.NET Core HTTP server metrics and .NET process runtime metrics using OpenTelemetry semantic convention names.

#### Scenario: ASP.NET metrics present

- **WHEN** `/metrics` is scraped
- **THEN** standard ASP.NET Core HTTP metrics are present (request duration, active requests)

#### Scenario: Process metrics present

- **WHEN** `/metrics` is scraped
- **THEN** .NET process metrics are present (CPU time, memory, GC collections, thread count)

### Requirement: No distributed tracing

The system SHALL NOT include any distributed tracing infrastructure. No `ActivitySource` definitions, no trace spans, no tracing exporter.

#### Scenario: No tracing overhead

- **WHEN** the application starts
- **THEN** no `ActivitySource` instances are registered and no tracing pipeline is configured
