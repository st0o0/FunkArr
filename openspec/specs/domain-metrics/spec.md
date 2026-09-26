# domain-metrics Specification

## Purpose
TBD - created by archiving change metrics-expansion. Update Purpose after archive.
## Requirements
### Requirement: Every domain SHALL expose a Meter via a static Telemetry class

Each domain project SHALL have a `Telemetry.cs` file containing a static class with an `internal static readonly Meter` named `FunkArr.<Domain>`. All instruments SHALL be declared as `internal static readonly` fields on that class.

#### Scenario: History domain has a Meter
- **WHEN** the application starts
- **THEN** `FunkArr.History` meter is registered and its instruments are available for collection

#### Scenario: RuleSet domain has a Meter
- **WHEN** the application starts
- **THEN** `FunkArr.RuleSet` meter is registered and its instruments are available for collection

### Requirement: All new meters SHALL be registered in TelemetrySetupContainer

The `TelemetrySetupContainer` SHALL call `.AddMeter()` for every domain meter, including `FunkArr.History` and `FunkArr.RuleSet`.

#### Scenario: New meters appear in Prometheus output
- **WHEN** the `/metrics` endpoint is scraped
- **THEN** instruments from all six domain meters and `FunkArr.ExternalApi` are present

### Requirement: Download domain SHALL instrument queue lifecycle and retry operations
The Download domain SHALL expose counters and gauges for queue lifecycle, retries, move failures, and scheduler/pause state.

#### Scenario: Download enqueued
- **WHEN** a download is added to the queue
- **THEN** `funkarr.download.enqueued_total` counter is incremented

#### Scenario: Download cancelled
- **WHEN** a download is cancelled or deleted
- **THEN** `funkarr.download.cancelled_total` counter is incremented

#### Scenario: Download retry
- **WHEN** a download worker retries a failed operation
- **THEN** `funkarr.download.retries_total` counter is incremented with a `reason` tag

#### Scenario: Phase duration recorded
- **WHEN** a download transitions between phases
- **THEN** `funkarr.download.phase_duration_seconds` histogram records the duration with a `phase` tag

#### Scenario: File move failure
- **WHEN** moving the completed file to its destination fails
- **THEN** `funkarr.download.move_failed_total` counter is incremented

#### Scenario: Schedule state observable
- **WHEN** Prometheus scrapes the metrics endpoint
- **THEN** `funkarr.download.schedule_enabled` gauge reports 0 or 1

#### Scenario: Pause state observable
- **WHEN** Prometheus scrapes the metrics endpoint
- **THEN** `funkarr.download.paused` gauge reports 0 or 1

#### Scenario: Duration distinguishes status
- **WHEN** a download completes or fails
- **THEN** `funkarr.download.duration_seconds` histogram records with a `status` tag (completed/failed)

### Requirement: Search domain SHALL instrument duration, failures, and API errors
The Search domain SHALL expose histograms for duration and result counts, and counters for timeouts, failures, and MediathekViewWeb errors.

#### Scenario: Search duration recorded
- **WHEN** a search completes (success or failure)
- **THEN** `funkarr.search.duration_seconds` histogram records the duration with `source` and `type` tags

#### Scenario: Search timeout counted
- **WHEN** a search times out
- **THEN** `funkarr.search.timeouts_total` counter is incremented

#### Scenario: Search failure counted
- **WHEN** a search fails
- **THEN** `funkarr.search.failed_total` counter is incremented with a `source` tag

#### Scenario: MediathekViewWeb API error counted
- **WHEN** the MediathekViewWeb HTTP request fails
- **THEN** `funkarr.search.mediathek_errors_total` counter is incremented

#### Scenario: Result count distribution tracked
- **WHEN** a search returns results
- **THEN** `funkarr.search.results_per_request` histogram records the count

### Requirement: Scoring domain SHALL instrument duration, runs, and error paths
The Scoring domain SHALL expose histograms for duration, counters for runs, regex timeouts, and no-config fallthrough, and tag existing counters with ruleSetId.

#### Scenario: Scoring duration recorded
- **WHEN** scoring completes for a batch
- **THEN** `funkarr.scoring.duration_seconds` histogram records the duration

#### Scenario: Scoring run counted
- **WHEN** scoring is invoked
- **THEN** `funkarr.scoring.runs_total` counter is incremented

#### Scenario: Regex timeout counted
- **WHEN** a regex evaluation times out
- **THEN** `funkarr.scoring.regex_timeouts_total` counter is incremented

#### Scenario: No-config fallthrough counted
- **WHEN** scoring is requested but no matching config exists
- **THEN** `funkarr.scoring.no_config_total` counter is incremented

#### Scenario: Accepted/rejected tagged by ruleSetId
- **WHEN** an item is accepted or rejected
- **THEN** the counter includes a `ruleSetId` tag

### Requirement: Enrichment domain SHALL instrument failures, duration, and cache state
The Enrichment domain SHALL expose counters for failures and items enriched, a histogram for API duration, and an observable gauge for cache sizes.

#### Scenario: Enrichment failure counted
- **WHEN** an enrichment API call fails
- **THEN** `funkarr.enrichment.failed_total` counter is incremented with an `api` tag

#### Scenario: Enrichment duration recorded
- **WHEN** an enrichment API call completes
- **THEN** `funkarr.enrichment.duration_seconds` histogram records with an `api` tag

#### Scenario: Items enriched counted
- **WHEN** items are successfully enriched
- **THEN** `funkarr.enrichment.items_enriched_total` counter is incremented with an `api` tag

#### Scenario: Cache size observable
- **WHEN** Prometheus scrapes the metrics endpoint
- **THEN** `funkarr.enrichment.cache_entries` gauge reports per-API cache sizes

### Requirement: History domain SHALL instrument recording and query operations
The History domain SHALL expose counters for recordings, trims, and queries by type.

#### Scenario: History recording counted
- **WHEN** a scoring history entry is persisted
- **THEN** `funkarr.history.recordings_total` counter is incremented

#### Scenario: History trim counted
- **WHEN** old history entries are trimmed
- **THEN** `funkarr.history.trimmed_total` counter is incremented

#### Scenario: History queries counted by type
- **WHEN** a history query is served
- **THEN** `funkarr.history.queries_total` counter is incremented with a `type` tag

### Requirement: RuleSet domain SHALL instrument lifecycle, updates, and file watching
The RuleSet domain SHALL expose counters for load, remove, update lifecycle, validation errors, scans, and file events, plus a gauge for active ruleset count.

#### Scenario: RuleSet loaded
- **WHEN** a ruleset is loaded
- **THEN** `funkarr.ruleset.loaded_total` counter is incremented with a `source` tag

#### Scenario: RuleSet removed
- **WHEN** a ruleset is removed
- **THEN** `funkarr.ruleset.removed_total` counter is incremented

#### Scenario: Active rulesets observable
- **WHEN** Prometheus scrapes the metrics endpoint
- **THEN** `funkarr.ruleset.active` gauge reports the count of active rulesets

#### Scenario: Update check counted
- **WHEN** a community update check runs
- **THEN** `funkarr.ruleset.update_checks_total` counter is incremented

#### Scenario: Update applied counted
- **WHEN** a community update is successfully applied
- **THEN** `funkarr.ruleset.updates_applied_total` counter is incremented

#### Scenario: Update error counted
- **WHEN** a community update fails
- **THEN** `funkarr.ruleset.update_errors_total` counter is incremented

#### Scenario: Validation error counted
- **WHEN** a ruleset fails validation
- **THEN** `funkarr.ruleset.validation_errors_total` counter is incremented

#### Scenario: Scan counted
- **WHEN** a scan or rescan is triggered
- **THEN** `funkarr.ruleset.scans_total` counter is incremented

#### Scenario: File events counted by type
- **WHEN** the filesystem watcher fires an event
- **THEN** `funkarr.ruleset.file_events_total` counter is incremented with a `type` tag

### Requirement: Metrics naming SHALL follow the funkarr convention

All metric names SHALL use the pattern `funkarr.<domain>.<metric_name>` with snake_case. Counters SHALL end with `_total`. Histograms measuring time SHALL end with `_seconds`. Tag names SHALL be short lowercase words.

#### Scenario: All metrics follow naming convention
- **WHEN** any new metric is defined
- **THEN** its name matches `funkarr\.[a-z]+\.[a-z_]+` and follows the suffix rules

