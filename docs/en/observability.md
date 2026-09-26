# Observability

FunkArr exposes a Prometheus-compatible `/metrics` endpoint. This lets you monitor download queues, scoring results, and external API health with any Prometheus-compatible monitoring stack.

## Metrics

### Application metrics

| Metric | Type | Description |
|--------|------|------------|
| `funkarr_search_requests_total` | Counter | Search requests (tag: `source`) |
| `funkarr_search_matches_total` | Counter | Search requests with results |
| `funkarr_search_no_match_total` | Counter | Search requests with no results |
| `funkarr_download_queue_size` | Gauge | Current queue depth |
| `funkarr_download_active` | Gauge | Currently active downloads |
| `funkarr_download_completed_total` | Counter | Completed downloads |
| `funkarr_download_failed_total` | Counter | Failed downloads (tag: `reason`) |
| `funkarr_download_bytes_total` | Counter | Total bytes downloaded |
| `funkarr_download_duration_seconds` | Histogram | Download duration |
| `funkarr_scoring_accepted_total` | Counter | Results accepted by scoring |
| `funkarr_scoring_rejected_total` | Counter | Results rejected by scoring |
| `funkarr_enrichment_requests_total` | Counter | Enrichment requests (tags: `api`, `status`) |
| `funkarr_external_api_requests_total` | Counter | External API calls (tags: `api`, `status_code`) |
| `funkarr_external_api_duration_seconds` | Histogram | External API latency (tag: `api`) |

### Standard metrics

ASP.NET Core HTTP metrics and .NET runtime metrics (CPU, memory, GC, threads) are also exported under their standard OpenTelemetry names. They are distinguished from other .NET services via the Prometheus `job` label in the scrape configuration.

## Configuration

The `/metrics` endpoint is always active and requires no configuration. It is available at `http://<host>:6969/metrics`.

## Prometheus scrape configuration

```yaml
scrape_configs:
  - job_name: funkarr
    static_configs:
      - targets: ['funkarr:6969']
```

## Grafana

The metrics can be visualized directly in Grafana. Example queries:

- **Queue depth**: `funkarr_download_queue_size`
- **Download rate**: `rate(funkarr_download_completed_total[5m])`
- **Search hit rate**: `rate(funkarr_search_matches_total[5m]) / rate(funkarr_search_requests_total[5m])`
- **External API latency (p95)**: `histogram_quantile(0.95, rate(funkarr_external_api_duration_seconds_bucket[5m]))`
