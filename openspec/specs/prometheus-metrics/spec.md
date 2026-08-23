## Purpose

Prometheus-compatible metrics instrumentation via `System.Diagnostics.Metrics` and `prometheus-net`, covering search, download, muxing, and API client subsystems.

## Requirements

### Requirement: Metrics singleton
The system SHALL provide a `FunkArrMetrics` singleton class with a `System.Diagnostics.Metrics.Meter` named `"FunkArr"`. The singleton SHALL be accessed via a static `Instance` field (`public static readonly`) with a private constructor.

#### Scenario: Meter identity
- **WHEN** any component accesses `FunkArrMetrics.Instance.Meter`
- **THEN** the meter name SHALL be `"FunkArr"`

#### Scenario: Singleton guarantee
- **WHEN** `FunkArrMetrics.Instance` is accessed from multiple components
- **THEN** all accesses SHALL return the same instance

### Requirement: Search metrics
The system SHALL instrument the search subsystem with the following metrics:
- `funkarr_search_total` — Counter<long> tracking completed searches, labeled by `type` (tv, movie, text) and `outcome` (success, error)
- `funkarr_search_duration_seconds` — Histogram<double> tracking search duration, labeled by `type`
- `funkarr_cache_hit_total` — Counter<long> tracking cache hits, labeled by `type`

#### Scenario: Search request counted
- **WHEN** a search request completes (success or failure)
- **THEN** `funkarr_search_total` SHALL be incremented with the appropriate `type` and `outcome` labels

#### Scenario: Search duration recorded
- **WHEN** a search request completes successfully
- **THEN** `funkarr_search_duration_seconds` SHALL record the elapsed time with the `type` label

#### Scenario: Cache hit counted
- **WHEN** SearchActor serves a result from cache instead of delegating to a child actor
- **THEN** `funkarr_cache_hit_total` SHALL be incremented with the `type` label

### Requirement: Download metrics
The system SHALL instrument the download subsystem with the following metrics:
- `funkarr_download_total` — Counter<long> tracking completed downloads, labeled by `outcome` (success, error, cancelled)
- `funkarr_download_duration_seconds` — Histogram<double> tracking download duration
- `funkarr_queue_depth` — Gauge<double> tracking current number of queued downloads

#### Scenario: Download completion counted
- **WHEN** a download completes (success, error, or cancellation)
- **THEN** `funkarr_download_total` SHALL be incremented with the appropriate `outcome` label

#### Scenario: Queue depth tracked
- **WHEN** a download is enqueued or completes
- **THEN** `funkarr_queue_depth` SHALL reflect the current queue size

### Requirement: Muxing metrics
The system SHALL instrument the muxing subsystem with:
- `funkarr_mux_duration_seconds` — Histogram<double> tracking FFmpeg muxing duration, labeled by `outcome` (success, error)

#### Scenario: Muxing duration recorded
- **WHEN** a muxing operation completes
- **THEN** `funkarr_mux_duration_seconds` SHALL record the elapsed time with the `outcome` label

### Requirement: API client metrics
The system SHALL instrument external API client calls with:
- `funkarr_api_call_total` — Counter<long> tracking API calls, labeled by `client` (mediathek, tvdb, tmdb, github) and `outcome` (success, error)
- `funkarr_api_call_duration_seconds` — Histogram<double> tracking call duration, labeled by `client`

#### Scenario: API call counted
- **WHEN** an external API call completes
- **THEN** `funkarr_api_call_total` SHALL be incremented with the `client` and `outcome` labels

#### Scenario: API call duration recorded
- **WHEN** an external API call completes
- **THEN** `funkarr_api_call_duration_seconds` SHALL record the elapsed time with the `client` label

### Requirement: Metrics extension methods per subsystem
Each metric category SHALL be defined as static extension methods on `FunkArrMetrics` in a dedicated file:
- `SearchMetricsExtensions.cs` — search metrics
- `DownloadMetricsExtensions.cs` — download metrics
- `MuxingMetricsExtensions.cs` — muxing metrics
- `ApiClientMetricsExtensions.cs` — API client metrics

#### Scenario: Extension method creates instrument
- **WHEN** an extension method (e.g., `AddSearchTotal`) is called on `FunkArrMetrics`
- **THEN** it SHALL return the corresponding `Counter`, `Histogram`, or `Gauge` instrument created from the meter

### Requirement: Prometheus scrape endpoint
The system SHALL expose a `/metrics` endpoint serving Prometheus-compatible scrape output via `prometheus-net.AspNetCore`. The meter adapter SHALL be configured to export only instruments from the `"FunkArr"` meter. HTTP request metrics SHALL be collected automatically via `UseHttpMetrics()`.

#### Scenario: Metrics endpoint accessible
- **WHEN** an HTTP GET request is made to `/metrics`
- **THEN** the response SHALL contain Prometheus text format metrics including all `funkarr_*` instruments

#### Scenario: Meter filter applied
- **WHEN** the meter adapter collects instruments
- **THEN** only instruments from the meter named `"FunkArr"` SHALL be exported
