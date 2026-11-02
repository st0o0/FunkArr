## MODIFIED Requirements

### Requirement: External API health metrics

The system SHALL expose metrics for external API calls via `TerpHttpMetricsHandler`
(Terp.Diagnostics) which adds `api` and `status_class` tags to the built-in
`System.Net.Http` meter's `http.client.request.duration` instrument.

The `TelemetrySetupContainer` SHALL register the meter `System.Net.Http` instead
of `FunkArr.ExternalApi`.

The setup containers for Enrichment and Search SHALL use
`TerpHttpMetricsHandler` instead of `ExternalApiMetricsHandler` for their named
HTTP clients (tmdb, tvdb, mediathekviewweb).

#### Scenario: External API call recorded

- **WHEN** an HTTP request to an external API (MediathekViewWeb, TMDB, TVDB) completes
- **THEN** `http_client_request_duration_seconds` is recorded with tags `api` (the client name) and `http_response_status_code` (the exact status code)

#### Scenario: External API latency recorded

- **WHEN** an HTTP request to an external API completes
- **THEN** `http_client_request_duration_seconds` histogram records the elapsed time with an `api` tag

#### Scenario: Untagged clients are filterable

- **WHEN** `/metrics` is scraped
- **THEN** external API metrics are distinguishable from other HTTP client metrics by filtering on `api!=""`
