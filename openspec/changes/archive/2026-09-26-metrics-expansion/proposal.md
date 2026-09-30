## Why

FunkArr has metrics in 4 of 6 domains, but coverage is shallow - only 15 instruments total, two domains (History, RuleSet) have zero metrics, and key operations like retries, failures, durations, and state gauges are missing across all instrumented domains. This makes operational monitoring incomplete: you can see downloads completed but not retries, searches run but not timeouts, scoring results but not duration or regex issues.

## What Changes

- **Download**: Add 7 new instruments (enqueued, cancelled, retries, phase duration, move failures, schedule/pause gauges) and add `status` tag to existing duration histogram
- **Search**: Add 5 new instruments (duration, timeouts, failures, MediathekViewWeb errors, results-per-request)
- **Scoring**: Add 4 new instruments (duration, runs, regex timeouts, no-config fallthrough) and add `ruleSetId` tag to existing accepted/rejected counters
- **Enrichment**: Add 4 new instruments (failures, duration, items enriched, cache entries gauge)
- **History**: New `Telemetry.cs` with 3 instruments (recordings, trims, queries)
- **RuleSet**: New `Telemetry.cs` with 9 instruments (loaded, removed, active gauge, update checks/applied/errors, validation errors, scans, file events)
- **Infrastructure**: Register `FunkArr.History` and `FunkArr.RuleSet` meters in `TelemetrySetupContainer`
- **Documentation**: Add metrics conventions to AGENTS.md

## Capabilities

### New Capabilities

- `domain-metrics`: Comprehensive metrics instrumentation across all FunkArr domains using System.Diagnostics.Metrics with Prometheus export

### Modified Capabilities

None.

## Impact

- `FunkArr.Download/Telemetry.cs` - extended with new instruments
- `FunkArr.Download/` - DownloadManager, DownloadWorker, DownloadScheduler, DownloadHistoryManager, FfmpegRunner updated with new metric calls
- `FunkArr.Search/Telemetry.cs` - extended with new instruments
- `FunkArr.Search/` - SearchManager, TvSearchWorker, MovieSearchWorker, MediathekViewWebManager updated
- `FunkArr.Scoring/Telemetry.cs` - extended with new instruments
- `FunkArr.Scoring/` - ScoringEngine, ScoringManager updated
- `FunkArr.Enrichment/Telemetry.cs` - extended with new instruments
- `FunkArr.Enrichment/` - TvdbEnrichmentActor, TmdbEnrichmentActor, TvdbClient, TmdbClient updated
- `FunkArr.History/Telemetry.cs` - new file
- `FunkArr.History/` - HistoryWorker, HistoryState updated
- `FunkArr.RuleSet/Telemetry.cs` - new file
- `FunkArr.RuleSet/` - RuleSetManager, RuleSetWorker, RuleSetUpdater updated
- `FunkArr/Configuration/TelemetrySetupContainer.cs` - two new meter registrations
- `AGENTS.md` - metrics conventions section added
