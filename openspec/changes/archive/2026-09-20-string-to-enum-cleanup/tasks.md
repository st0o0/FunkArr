## 1. Core Enum Definitions

- [x] 1.1 Create `SearchSource` enum in `FunkArr.Messages` with `Sonarr`, `Radarr`, `Prowlarr`, `Ui`, `Test` members and `JsonStringEnumConverter`/`JsonStringEnumMemberName` attributes
- [x] 1.2 Create `MediaType` enum in `FunkArr.Messages` with `Show`, `Movie` members and `JsonStringEnumConverter`/`JsonStringEnumMemberName("show")`/`JsonStringEnumMemberName("movie")` attributes
- [x] 1.3 Remove `MediaType` enum from `FunkArr.Api.Models.RuleSetListEntry` and update `RuleSetListEntry` to use `FunkArr.Messages.MediaType`
- [x] 1.4 Remove `SourceType` enum stays (it's a different concept) — verify no collision with the new types

## 2. Messages — SearchSource

- [x] 2.1 Change `SearchCommand.Source` from `string` to `SearchSource`
- [x] 2.2 Change `SearchRequest.Source` from `string` to `SearchSource` (affects `SearchSeries`, `SearchMovie`)
- [x] 2.3 Change `ScoringOrigin.Source` from `string` to `SearchSource`

## 3. Messages — MediaType (Download)

- [x] 3.1 Change `AddDownload.Category` from `string` to `MediaType`
- [x] 3.2 Change `InitDownload.Category` from `string` to `MediaType`
- [x] 3.3 Change `RecordDownload.Category` from `string` to `MediaType`
- [x] 3.4 Change `QueueResult.QueueItem.Category` from `string` to `MediaType`
- [x] 3.5 Change `WorkerStatusResult.Category` from `string` to `MediaType`
- [x] 3.6 Change `QueryQueue.Category` from `string?` to `MediaType?`
- [x] 3.7 Change `QueryHistory.Category` from `string?` to `MediaType?`
- [x] 3.8 Change `HistoryResult.HistoryItem.Category` from `string` to `MediaType`

## 4. Persistence DTOs

- [x] 4.1 Change `DownloadInitialized.Category` from `string` to `MediaType`
- [x] 4.2 Change `HistoryRecorded.Category` from `string` to `MediaType`
- [x] 4.3 Change `ScoringRecorded.Source` from `string` to `SearchSource`

## 5. Search Domain

- [x] 5.1 Change `TvSearchWorkerState.Source` from `string` to `SearchSource`
- [x] 5.2 Change `MovieSearchWorkerState.Source` from `string` to `SearchSource`
- [x] 5.3 Change `ReleaseVariant.Expand` parameter `mediaType` from `string` to `MediaType`
- [x] 5.4 Change `ReleaseTitleBuilder.Build` parameter `mediaType` from `string` to `MediaType`
- [x] 5.5 Update `TvSearchWorkerState.ToSearchCompleted()` to pass `MediaType.Show` instead of `"tv"`
- [x] 5.6 Update `MovieSearchWorkerState` equivalent to pass `MediaType.Movie`
- [x] 5.7 Update `SearchManager` to pass `SearchSource` enum values

## 6. Scoring Domain

- [x] 6.1 Update `ScoringActor` — replace `msg.Origin.Source == "Test"` with `msg.Origin.Source == SearchSource.Test`
- [x] 6.2 Update `ScoringHistoryState` to work with `SearchSource` enum in `ScoringRecorded` mapping

## 7. Download Domain

- [x] 7.1 Update `DownloadWorkerState` to use `MediaType` for Category
- [x] 7.2 Update `DownloadManagerState` / `DownloadManagerStateExtensions` for `MediaType?` filter
- [x] 7.3 Update `DownloadHistoryManagerState` to use `MediaType` for Category

## 8. API Layer

- [x] 8.1 Remove local `MediaType` enum from `FunkArr.Api.Models`, add `using FunkArr.Messages` where needed
- [x] 8.2 Simplify `RuleSetMappingExtensions` — remove manual `"show"`/`"movie"` string switch, use direct enum from deserialized ruleset
- [x] 8.3 Verify API serialization — `MediaType` now serializes as `"show"`/`"movie"` strings (0.x breaking change OK)

## 9. ArrApi Layer

- [x] 9.1 Update Newznab adapter to map Newznab category int to `SearchSource` enum for `SearchCommand.Source`
- [x] 9.2 Update SABnzbd adapter to map `MediaType` enum to/from Newznab `"tv"`/`"movies"` category strings

## 10. Tests

- [x] 10.1 Update `FunkArr.Search.Tests` — replace all `"sonarr"` / `"tv"` string literals with enum values
- [x] 10.2 Update `FunkArr.Download.Tests` — replace `"tv"` / `"movies"` string literals with `MediaType.Show` / `MediaType.Movie`
- [x] 10.3 Update `FunkArr.Scoring.Tests` — replace `"test"` / `"sonarr"` string literals with enum values
- [x] 10.4 Update `FunkArr.Api.Tests` — replace string literals with enum values
- [x] 10.5 Update `FunkArr.ArrApi.Tests` — adjust for enum-typed messages
- [x] 10.6 Build solution and run `dotnet format`
- [x] 10.7 Run all test projects and verify green
