## ADDED Requirements

### Requirement: Cache metrics on Enrichment meter

The `FunkArr.Enrichment` meter SHALL expose cache observability via `MemoryCacheStatistics`:

- `funkarr.enrichment.cache_entries` — ObservableGauge reading `CurrentEntryCount`
- `funkarr.enrichment.cache_hits_total` — ObservableCounter reading `TotalHits`
- `funkarr.enrichment.cache_misses_total` — ObservableCounter reading `TotalMisses`

The Enrichment `Telemetry` class SHALL accept an `IMemoryCache` reference to read statistics from.

#### Scenario: Cache entries gauge reports current count

- **WHEN** the metrics endpoint is scraped
- **THEN** `funkarr.enrichment.cache_entries` SHALL return the value of `MemoryCacheStatistics.CurrentEntryCount`

#### Scenario: Cache hits counter reports total hits

- **WHEN** the metrics endpoint is scraped
- **THEN** `funkarr.enrichment.cache_hits_total` SHALL return the value of `MemoryCacheStatistics.TotalHits`

#### Scenario: Cache misses counter reports total misses

- **WHEN** the metrics endpoint is scraped
- **THEN** `funkarr.enrichment.cache_misses_total` SHALL return the value of `MemoryCacheStatistics.TotalMisses`

### Requirement: Cache meter registration in TelemetrySetupContainer

TelemetrySetupContainer SHALL register the `FunkArr.Enrichment` meter (if not already registered) so cache metrics are exported via Prometheus.

#### Scenario: Meter is exported

- **WHEN** Prometheus scrapes the metrics endpoint
- **THEN** `funkarr_enrichment_cache_entries`, `funkarr_enrichment_cache_hits_total`, and `funkarr_enrichment_cache_misses_total` SHALL be present
