# Observability

FunkArr stellt einen Prometheus-kompatiblen `/metrics`-Endpunkt bereit. Damit lassen sich Downloads, Suchen, Scoring, Enrichment, Rulesets und externe API-Aufrufe mit jedem Prometheus-kompatiblen Monitoring-Stack ueberwachen.

## Metriken

### Download (`FunkArr.Download`)

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_download_completed_total` | Counter | Abgeschlossene Downloads |
| `funkarr_download_failed_total` | Counter | Fehlgeschlagene Downloads (Tag: `reason`) |
| `funkarr_download_duration_seconds` | Histogram | Download-Dauer (Tag: `status`) |
| `funkarr_download_bytes_total` | Counter | Heruntergeladene Bytes |
| `funkarr_download_enqueued_total` | Counter | In die Queue aufgenommene Downloads |
| `funkarr_download_cancelled_total` | Counter | Abgebrochene Downloads |
| `funkarr_download_retries_total` | Counter | Wiederholungsversuche (Tag: `reason`) |
| `funkarr_download_move_failed_total` | Counter | Fehlgeschlagene Dateiverschiebungen |
| `funkarr_download_queue_size` | Gauge | Aktuelle Queue-Tiefe |
| `funkarr_download_active` | Gauge | Aktuell laufende Downloads |
| `funkarr_download_paused` | Gauge | Downloads pausiert (0/1) |
| `funkarr_download_schedule_enabled` | Gauge | Zeitplan aktiv (0/1) |

### Search (`FunkArr.Search`)

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_search_requests_total` | Counter | Suchanfragen (Tag: `source`) |
| `funkarr_search_matches_total` | Counter | Suchanfragen mit akzeptierten Ergebnissen (Tag: `source`) |
| `funkarr_search_no_match_total` | Counter | Suchanfragen ohne akzeptierte Ergebnisse (Tag: `source`) |
| `funkarr_search_duration_seconds` | Histogram | End-to-End Suchdauer (Tags: `source`, `type`) |
| `funkarr_search_timeouts_total` | Counter | Such-Timeouts |
| `funkarr_search_failed_total` | Counter | Fehlgeschlagene Suchen (Tag: `source`) |
| `funkarr_search_mediathek_errors_total` | Counter | MediathekViewWeb API-Fehler |
| `funkarr_search_results_per_request` | Histogram | Ergebnisanzahl pro Suchanfrage |

### Scoring (`FunkArr.Scoring`)

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_scoring_accepted_total` | Counter | Vom Scoring akzeptierte Ergebnisse (Tag: `ruleSetId`) |
| `funkarr_scoring_rejected_total` | Counter | Vom Scoring abgelehnte Ergebnisse (Tag: `ruleSetId`) |
| `funkarr_scoring_duration_seconds` | Histogram | Scoring-Dauer |
| `funkarr_scoring_runs_total` | Counter | Scoring-Aufrufe |
| `funkarr_scoring_regex_timeouts_total` | Counter | Regex-Timeouts |
| `funkarr_scoring_no_config_total` | Counter | Anfragen ohne passende Konfiguration |

### Enrichment (`FunkArr.Enrichment`)

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_enrichment_requests_total` | Counter | Enrichment-Anfragen (Tags: `api`, `status`) |
| `funkarr_enrichment_failed_total` | Counter | Fehlgeschlagene Enrichments (Tag: `api`) |
| `funkarr_enrichment_duration_seconds` | Histogram | API-Aufruf-Dauer (Tag: `api`) |
| `funkarr_enrichment_items_enriched_total` | Counter | Angereicherte Items (Tag: `api`) |
| `funkarr_enrichment_cache_entries` | Gauge | Cache-Eintraege (Tag: `api`) |

### History (`FunkArr.History`)

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_history_recordings_total` | Counter | Aufgezeichnete Scoring-Ergebnisse |
| `funkarr_history_trimmed_total` | Counter | Bereingte alte Eintraege |
| `funkarr_history_queries_total` | Counter | Abgerufene History-Anfragen (Tag: `type`) |

### RuleSet (`FunkArr.RuleSet`)

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_ruleset_loaded_total` | Counter | Geladene Rulesets (Tag: `source`) |
| `funkarr_ruleset_removed_total` | Counter | Entfernte Rulesets |
| `funkarr_ruleset_active` | Gauge | Aktive Rulesets |
| `funkarr_ruleset_update_checks_total` | Counter | Community-Update-Pruefungen |
| `funkarr_ruleset_updates_applied_total` | Counter | Angewendete Community-Updates |
| `funkarr_ruleset_update_errors_total` | Counter | Fehlgeschlagene Community-Updates |
| `funkarr_ruleset_validation_errors_total` | Counter | Validierungsfehler |
| `funkarr_ruleset_scans_total` | Counter | Scan/Rescan-Aufrufe |
| `funkarr_ruleset_file_events_total` | Counter | Dateisystem-Watcher-Events (Tag: `type`) |

### Externe API (`FunkArr.ExternalApi`)

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_external_api_requests_total` | Counter | Externe API-Aufrufe (Tags: `api`, `status_code`) |
| `funkarr_external_api_duration_seconds` | Histogram | Externe API-Latenz (Tag: `api`) |

### Standardmetriken

Zusaetzlich werden ASP.NET Core HTTP-Metriken und .NET Runtime-Metriken (CPU, Speicher, GC, Threads) unter ihren Standard-OpenTelemetry-Namen exportiert. Die Unterscheidung zu anderen .NET-Services erfolgt ueber das Prometheus `job`-Label in der Scrape-Konfiguration.

## Konfiguration

Der `/metrics`-Endpunkt ist immer aktiv und erfordert keine Konfiguration. Er ist unter `http://<host>:6969/metrics` erreichbar.

## Prometheus Scrape-Konfiguration

```yaml
scrape_configs:
  - job_name: funkarr
    static_configs:
      - targets: ['funkarr:6969']
```

## Grafana

Die Metriken lassen sich direkt in Grafana visualisieren. Beispiel-Queries:

- **Queue-Tiefe**: `funkarr_download_queue_size`
- **Download-Rate**: `rate(funkarr_download_completed_total[5m])`
- **Sucherfolgsrate**: `rate(funkarr_search_matches_total[5m]) / rate(funkarr_search_requests_total[5m])`
- **Scoring-Dauer (p95)**: `histogram_quantile(0.95, rate(funkarr_scoring_duration_seconds_bucket[5m]))`
- **Externe API-Latenz (p95)**: `histogram_quantile(0.95, rate(funkarr_external_api_duration_seconds_bucket[5m]))`
- **Enrichment Cache-Trefferrate**: `rate(funkarr_enrichment_requests_total{status="hit"}[5m]) / rate(funkarr_enrichment_requests_total[5m])`
- **Aktive Rulesets**: `funkarr_ruleset_active`
- **Download-Wiederholungsrate**: `rate(funkarr_download_retries_total[5m])`
