## 1. New domain Telemetry classes

- [x] 1.1 Create `FunkArr.History/Telemetry.cs` with Meter `FunkArr.History` and instruments: `recordings_total` (Counter), `trimmed_total` (Counter), `queries_total` (Counter, tag: type)
- [x] 1.2 Create `FunkArr.RuleSet/Telemetry.cs` with Meter `FunkArr.RuleSet` and instruments: `loaded_total` (Counter, tag: source), `removed_total` (Counter), `active` (ObservableGauge), `update_checks_total` (Counter), `updates_applied_total` (Counter), `update_errors_total` (Counter), `validation_errors_total` (Counter), `scans_total` (Counter), `file_events_total` (Counter, tag: type)
- [x] 1.3 Register `FunkArr.History` and `FunkArr.RuleSet` meters in `TelemetrySetupContainer.cs`

## 2. Download domain metrics expansion

- [x] 2.1 Add instruments to `FunkArr.Download/Telemetry.cs`: `enqueued_total`, `cancelled_total`, `retries_total` (tag: reason), `move_failed_total`, `schedule_enabled` (ObservableGauge), `paused` (ObservableGauge)
- [x] 2.2 Add `status` tag to existing `duration_seconds` histogram
- [x] 2.3 Instrument `DownloadManager`: increment `enqueued_total` in HandleAdd, `cancelled_total` in HandleDelete/HandleCancel, set `paused` gauge callback
- [x] 2.4 Instrument `DownloadWorker`: increment `retries_total` in HandleRetry, increment `move_failed_total` on file move failure
- [x] 2.5 Instrument `DownloadScheduler`: set `schedule_enabled` gauge callback
- [x] 2.6 Update `FfmpegRunner` to pass `status` tag to `duration_seconds`
- Deferred: `phase_duration_seconds` - requires state record changes for per-phase timestamps, not just metric recording

## 3. Search domain metrics expansion

- [x] 3.1 Add instruments to `FunkArr.Search/Telemetry.cs`: `duration_seconds` (tags: source, type), `timeouts_total`, `failed_total` (tag: source), `mediathek_errors_total`, `results_per_request`
- [x] 3.2 Instrument `SearchManager`: increment `timeouts_total` in HandleTimeout, `failed_total` in HandleFailed
- [x] 3.3 Instrument `TvSearchWorker` and `MovieSearchWorker`: record `duration_seconds`, record `results_per_request`
- [x] 3.4 Instrument `MediathekViewWebManager`: increment `mediathek_errors_total` in HandleHttpFailed

## 4. Scoring domain metrics expansion

- [x] 4.1 Add instruments to `FunkArr.Scoring/Telemetry.cs`: `duration_seconds`, `runs_total`, `regex_timeouts_total`, `no_config_total`
- [x] 4.2 Add `ruleSetId` tag to existing `accepted_total` and `rejected_total` counters
- [x] 4.3 Instrument `ScoringEngine`: record `duration_seconds`, increment `runs_total`, increment `regex_timeouts_total` in regex timeout catch
- [x] 4.4 Instrument `ScoringManager`: increment `no_config_total` in HandleScoreItems when no config found

## 5. Enrichment domain metrics expansion

- [x] 5.1 Add instruments to `FunkArr.Enrichment/Telemetry.cs`: `failed_total` (tag: api), `duration_seconds` (tag: api), `items_enriched_total` (tag: api), `cache_entries` (ObservableGauge, tag: api)
- [x] 5.2 Instrument `TvdbEnrichmentActor` and `TmdbEnrichmentActor`: increment `failed_total` in failure catches, increment `items_enriched_total`
- [x] 5.3 Instrument `TvdbClient` and `TmdbClient`: record `duration_seconds` around API calls
- [x] 5.4 Set `cache_entries` gauge callbacks reading from `TvdbClient.CacheEntryCount` and `TmdbClient.CacheEntryCount`

## 6. History domain instrumentation

- [x] 6.1 Instrument `HistoryWorker`: increment `recordings_total` in HandleRecordHistory, increment `queries_total` with type tag in query handlers
- [x] 6.2 Instrument `HistoryWorker`: increment `trimmed_total` when Trim removes entries

## 7. RuleSet domain instrumentation

- [x] 7.1 Instrument `RuleSetWorker`: increment `loaded_total` in HandleLoad with source tag, increment `removed_total` in HandleRemove, increment `validation_errors_total` in validation failures
- [x] 7.2 Instrument `RuleSetManager`: set `active` gauge callback, increment `scans_total` in HandleScan/HandleFullRescan, increment `file_events_total` in file watcher handlers
- [x] 7.3 Instrument `RuleSetUpdater`: increment `update_checks_total` in HandleCheckForUpdates, increment `updates_applied_total` on success, increment `update_errors_total` on failure

## 8. Documentation

- [x] 8.1 Add metrics conventions section to AGENTS.md covering naming pattern, Telemetry.cs structure, instrument types, tag rules, and complete metrics inventory

## 9. Verify

- [x] 9.1 `dotnet build src/FunkArr.slnx`
- [x] 9.2 Run all test projects (842 tests, 0 failures)
- [x] 9.3 `dotnet format src/FunkArr.slnx --verify-no-changes` (pre-existing violations only)
