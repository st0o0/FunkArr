# Observability

FunkArr stellt einen Prometheus-kompatiblen `/metrics`-Endpunkt bereit. Damit lassen sich Download-Queues, Scoring-Ergebnisse und externe API-Gesundheit mit jedem Prometheus-kompatiblen Monitoring-Stack uberwachen.

## Metriken

### Anwendungsmetriken

| Metrik | Typ | Beschreibung |
|--------|-----|-------------|
| `funkarr_search_requests_total` | Counter | Suchanfragen (Tag: `source`) |
| `funkarr_search_matches_total` | Counter | Suchanfragen mit Ergebnissen |
| `funkarr_search_no_match_total` | Counter | Suchanfragen ohne Ergebnis |
| `funkarr_download_queue_size` | Gauge | Aktuelle Queue-Tiefe |
| `funkarr_download_active` | Gauge | Aktuell laufende Downloads |
| `funkarr_download_completed_total` | Counter | Abgeschlossene Downloads |
| `funkarr_download_failed_total` | Counter | Fehlgeschlagene Downloads (Tag: `reason`) |
| `funkarr_download_bytes_total` | Counter | Heruntergeladene Bytes |
| `funkarr_download_duration_seconds` | Histogram | Download-Dauer |
| `funkarr_scoring_accepted_total` | Counter | Vom Scoring akzeptierte Ergebnisse |
| `funkarr_scoring_rejected_total` | Counter | Vom Scoring abgelehnte Ergebnisse |
| `funkarr_enrichment_requests_total` | Counter | Enrichment-Anfragen (Tags: `api`, `status`) |
| `funkarr_external_api_requests_total` | Counter | Externe API-Aufrufe (Tags: `api`, `status_code`) |
| `funkarr_external_api_duration_seconds` | Histogram | Externe API-Latenz (Tag: `api`) |

### Standardmetriken

Zusatzlich werden ASP.NET Core HTTP-Metriken und .NET Runtime-Metriken (CPU, Speicher, GC, Threads) unter ihren Standard-OpenTelemetry-Namen exportiert. Die Unterscheidung zu anderen .NET-Services erfolgt uber das Prometheus `job`-Label in der Scrape-Konfiguration.

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
- **Externe API-Latenz (p95)**: `histogram_quantile(0.95, rate(funkarr_external_api_duration_seconds_bucket[5m]))`
