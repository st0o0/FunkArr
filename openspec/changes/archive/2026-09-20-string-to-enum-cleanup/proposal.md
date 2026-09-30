## Why

Multiple string fields across Messages, Search, Scoring, and Download carry values from a closed set (`"sonarr"`, `"radarr"`, `"tv"`, `"movie"`, `"show"`) but are typed as `string`, making them error-prone and inconsistent. The same concept (series vs film) uses three different labels depending on context (`"show"` in rulesets, `"tv"` in the download pipeline, `MediaType.Show` in the API layer). Since we're already making schema changes this is the right moment to clean these up.

## What Changes

- **BREAKING**: Introduce `SearchSource` enum in `FunkArr.Core` with members `Sonarr`, `Radarr`, `Prowlarr`, `Ui`, `Test`. Replace `string Source` in `SearchCommand`, `SearchRequest`, `ScoringOrigin`, `ScoringRecorded`, and worker states.
- **BREAKING**: Move `MediaType` enum from `FunkArr.Api.Models` to `FunkArr.Core` with `JsonStringEnumMemberName` attributes (`"show"` / `"movie"`) for ruleset JSON compatibility. Replace `string Category` in `AddDownload`, `InitDownload`, `RecordDownload`, `QueueResult`, `WorkerStatusResult`, `QueryQueue`, `QueryHistory`, `HistoryResult`. Replace `string mediaType` parameter in `ReleaseVariant.Expand` and `ReleaseTitleBuilder.Build`.
- Newznab category mapping (`MediaType.Show` <-> `"tv"`) stays in the ArrApi translation layer.
- Update all tests to use enum values instead of string literals.
- Update persistence DTOs: new versions of `ScoringRecorded` and `DownloadInitialized`/`HistoryRecorded` with enum fields.

## Capabilities

### New Capabilities
- `domain-enums`: Core enum types (`SearchSource`, `MediaType`) shared across all domain projects, with JSON serialization attributes for external format compatibility.

### Modified Capabilities
- `search-messages`: `SearchCommand.Source` and `SearchRequest.Source` change from `string` to `SearchSource` enum. `ScoringOrigin.Source` changes from `string` to `SearchSource`.
- `search-pipeline-types`: `ReleaseVariant.Expand` and `ReleaseTitleBuilder.Build` accept `MediaType` enum instead of `string mediaType`.
- `download-messages`: `AddDownload.Category`, `InitDownload.Category`, `RecordDownload.Category`, `QueueResult.Category`, `WorkerStatusResult.Category` change from `string` to `MediaType`. `QueryQueue.Category` and `QueryHistory.Category` change from `string?` to `MediaType?`.
- `api-enum-types`: `MediaType` enum moves from `FunkArr.Api.Models` to `FunkArr.Core`. API layer uses the shared enum and maps to int for JSON serialization as before.
- `scoring-trace-persistence`: `ScoringRecorded.Source` changes from `string` to `SearchSource` enum. New DTO version needed.

## Impact

- **Messages**: `SearchCommand`, `SearchRequest`, `SearchSeries`, `SearchMovie`, `ScoringOrigin`, `AddDownload`, `InitDownload`, `RecordDownload`, `QueueResult`, `WorkerStatusResult`, `QueryQueue`, `QueryHistory`, `HistoryResult`
- **Search domain**: `TvSearchWorkerState`, `MovieSearchWorkerState`, `ReleaseVariant`, `ReleaseTitleBuilder`, `SearchManager`
- **Scoring domain**: `ScoringActor`, `ScoringHistoryState`
- **Download domain**: `DownloadWorkerState`, `DownloadManagerState`, `DownloadHistoryManagerState`
- **API layer**: `RuleSetMappingExtensions` (simplifies — no more string switch), `RuleSetListEntry` (uses shared `MediaType`)
- **ArrApi layer**: Translation from Newznab categories to `MediaType` enum
- **Persistence**: `ScoringRecorded`, `DownloadInitialized`, `HistoryRecorded` DTOs need new versions
- **Tests**: All test projects that construct messages with string Source/Category/MediaType literals
