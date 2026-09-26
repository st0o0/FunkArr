# Observability

FunkArr exposes a Prometheus-compatible `/metrics` endpoint. Use it with any Prometheus-compatible monitoring stack to observe download queues, search activity, scoring results, enrichment caching, ruleset management, and external API health.

## Metrics

### Download (`FunkArr.Download`)

| Metric | Type | Description |
|--------|------|-------------|
| `funkarr_download_completed_total` | Counter | Completed downloads |
| `funkarr_download_failed_total` | Counter | Failed downloads (tag: `reason`) |
| `funkarr_download_duration_seconds` | Histogram | Download duration (tag: `status`) |
| `funkarr_download_bytes_total` | Counter | Total bytes downloaded |
| `funkarr_download_enqueued_total` | Counter | Downloads added to queue |
| `funkarr_download_cancelled_total` | Counter | Downloads cancelled |
| `funkarr_download_retries_total` | Counter | Retry attempts (tag: `reason`) |
| `funkarr_download_move_failed_total` | Counter | File move failures |
| `funkarr_download_queue_size` | Gauge | Current queue depth |
| `funkarr_download_active` | Gauge | Currently active downloads |
| `funkarr_download_paused` | Gauge | Whether downloads are paused (0/1) |
| `funkarr_download_schedule_enabled` | Gauge | Whether schedule is active (0/1) |

### Search (`FunkArr.Search`)

| Metric | Type | Description |
|--------|------|-------------|
| `funkarr_search_requests_total` | Counter | Search requests (tag: `source`) |
| `funkarr_search_matches_total` | Counter | Searches with accepted results (tag: `source`) |
| `funkarr_search_no_match_total` | Counter | Searches with no accepted results (tag: `source`) |
| `funkarr_search_duration_seconds` | Histogram | End-to-end search duration (tags: `source`, `type`) |
| `funkarr_search_timeouts_total` | Counter | Search timeouts |
| `funkarr_search_failed_total` | Counter | Search failures (tag: `source`) |
| `funkarr_search_mediathek_errors_total` | Counter | MediathekViewWeb API errors |
| `funkarr_search_results_per_request` | Histogram | Result count distribution |

### Scoring (`FunkArr.Scoring`)

| Metric | Type | Description |
|--------|------|-------------|
| `funkarr_scoring_accepted_total` | Counter | Accepted results (tag: `ruleSetId`) |
| `funkarr_scoring_rejected_total` | Counter | Rejected results (tag: `ruleSetId`) |
| `funkarr_scoring_duration_seconds` | Histogram | Scoring duration |
| `funkarr_scoring_runs_total` | Counter | Scoring invocations |
| `funkarr_scoring_regex_timeouts_total` | Counter | Regex match timeouts |
| `funkarr_scoring_no_config_total` | Counter | Requests with no matching config |

### Enrichment (`FunkArr.Enrichment`)

| Metric | Type | Description |
|--------|------|-------------|
| `funkarr_enrichment_requests_total` | Counter | Enrichment requests (tags: `api`, `status`) |
| `funkarr_enrichment_failed_total` | Counter | Enrichment failures (tag: `api`) |
| `funkarr_enrichment_duration_seconds` | Histogram | API call duration (tag: `api`) |
| `funkarr_enrichment_items_enriched_total` | Counter | Items enriched (tag: `api`) |
| `funkarr_enrichment_cache_entries` | Gauge | Cache entry count (tag: `api`) |

### History (`FunkArr.History`)

| Metric | Type | Description |
|--------|------|-------------|
| `funkarr_history_recordings_total` | Counter | Scoring history recordings |
| `funkarr_history_trimmed_total` | Counter | Old entries trimmed |
| `funkarr_history_queries_total` | Counter | History queries served (tag: `type`) |

### RuleSet (`FunkArr.RuleSet`)

| Metric | Type | Description |
|--------|------|-------------|
| `funkarr_ruleset_loaded_total` | Counter | Rulesets loaded (tag: `source`) |
| `funkarr_ruleset_removed_total` | Counter | Rulesets removed |
| `funkarr_ruleset_active` | Gauge | Active ruleset count |
| `funkarr_ruleset_update_checks_total` | Counter | Community update checks |
| `funkarr_ruleset_updates_applied_total` | Counter | Community updates applied |
| `funkarr_ruleset_update_errors_total` | Counter | Community update errors |
| `funkarr_ruleset_validation_errors_total` | Counter | Ruleset validation errors |
| `funkarr_ruleset_scans_total` | Counter | Scan/rescan invocations |
| `funkarr_ruleset_file_events_total` | Counter | Filesystem watcher events (tag: `type`) |

### External API (`FunkArr.ExternalApi`)

| Metric | Type | Description |
|--------|------|-------------|
| `funkarr_external_api_requests_total` | Counter | External API calls (tags: `api`, `status_code`) |
| `funkarr_external_api_duration_seconds` | Histogram | External API latency (tag: `api`) |

### Standard metrics

ASP.NET Core HTTP metrics and .NET runtime metrics (CPU, memory, GC, threads) are exported under their standard OpenTelemetry names. Distinguish from other .NET services via the Prometheus `job` label in the scrape configuration.

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
- **Search success rate**: `rate(funkarr_search_matches_total[5m]) / rate(funkarr_search_requests_total[5m])`
- **Scoring duration (p95)**: `histogram_quantile(0.95, rate(funkarr_scoring_duration_seconds_bucket[5m]))`
- **External API latency (p95)**: `histogram_quantile(0.95, rate(funkarr_external_api_duration_seconds_bucket[5m]))`
- **Enrichment cache hit rate**: `rate(funkarr_enrichment_requests_total{status="hit"}[5m]) / rate(funkarr_enrichment_requests_total[5m])`
- **Active rulesets**: `funkarr_ruleset_active`
- **Download retry rate**: `rate(funkarr_download_retries_total[5m])`
