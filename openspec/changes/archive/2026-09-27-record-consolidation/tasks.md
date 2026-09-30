## 1. ExternalIds

- [x] 1.1 Create `ExternalIds` record in `FunkArr.Messages/Shared/ExternalIds.cs` and `PersistedExternalIds` in `FunkArr.Persistence/Events/Shared/PersistedExternalIds.cs`
- [x] 1.2 Update `ResolveRuleSet`, `RegisterRuleSet`, `RegisteredRuleSetEntry`, `QueryRegisteredRuleSets`, `LocalRuleSetCommands` to embed `ExternalIds`
- [x] 1.3 Update `RuleSetIdentity` (in both `FunkArr.Messages/RuleSet/` and `FunkArr.Api/Models/`) to embed `ExternalIds`
- [x] 1.4 Update all RuleSet domain construction sites and mapping extensions
- [x] 1.5 Update RuleSet and API test projects, build and run all tests

## 2. DownloadMedia

- [x] 2.1 Create `DownloadMedia` record in `FunkArr.Messages/Shared/DownloadMedia.cs` and `PersistedDownloadMedia` in `FunkArr.Persistence/Events/Shared/PersistedDownloadMedia.cs`
- [x] 2.2 Update `AddDownload` to embed `DownloadMedia` (+ Priority as separate field)
- [x] 2.3 Update `InitDownload` to embed `DownloadMedia` (+ DownloadId, RouteName, ProxyUrl as separate fields)
- [x] 2.4 Update `DownloadInitialized` to embed `PersistedDownloadMedia` (+ DownloadId, RouteName, ProxyUrl)
- [x] 2.5 Update `DownloadWorkerState` to embed `DownloadMedia` (replacing Title/VideoUrl/SubtitleUrl/Channel/Duration/Size/Category), update `Empty` factory and all `Apply` extensions
- [x] 2.6 Add mapping extensions for `PersistedDownloadMedia` <-> `DownloadMedia` in Download domain
- [x] 2.7 Update all Download domain construction sites (DownloadManager, DownloadWorker, state mappings)
- [x] 2.8 Update Download and ArrApi test projects, build and run all tests

## 3. DownloadProgress

- [x] 3.1 Create `DownloadProgress` record in `FunkArr.Messages/Shared/DownloadProgress.cs`
- [x] 3.2 Update `DownloadWorkerState` to embed `DownloadProgress` (replacing BytesDownloaded/CurrentTimeUs/Speed), update `Empty` factory and `Apply` for `DownloadAttemptStarted`
- [x] 3.3 Update `WorkerStatusResult` and `QueueItem` to embed `DownloadProgress`
- [x] 3.4 Update snapshot/query mapping methods that produce WorkerStatusResult and QueueItem
- [x] 3.5 Update Download test projects, build and run all tests

## 4. DownloadCompletion

- [x] 4.1 Create `DownloadCompletion` record in `FunkArr.Messages/Shared/DownloadCompletion.cs` and `PersistedDownloadCompletion` in `FunkArr.Persistence/Events/Shared/PersistedDownloadCompletion.cs`
- [x] 4.2 Update `RecordDownload` to embed `DownloadCompletion` (+ DownloadId as separate field)
- [x] 4.3 Update `HistoryItem` to embed `DownloadCompletion` (+ DownloadId as separate field)
- [x] 4.4 Update `DownloadHistoryRecorded` to embed `PersistedDownloadCompletion` (+ DownloadId)
- [x] 4.5 Update `DownloadWorkerState.ToRecordDownload()` to construct `DownloadCompletion`
- [x] 4.6 Add mapping extensions for `PersistedDownloadCompletion` <-> `DownloadCompletion`
- [x] 4.7 Update History domain construction sites and persistence mappings
- [x] 4.8 Update Download, History, and Api test projects, build and run all tests

## 5. ScoreCandidate embedding

- [x] 5.1 Create `PersistedScoreCandidate` in `FunkArr.Persistence/Events/Shared/PersistedScoreCandidate.cs`
- [x] 5.2 Update `ItemTrace` to embed `ScoreCandidate Candidate` replacing the seven `Candidate*` prefixed fields
- [x] 5.3 Update `PersistedItemTrace` to embed `PersistedScoreCandidate Candidate` replacing the seven `Candidate*` prefixed fields
- [x] 5.4 Update `ScoringEngine` construction of ItemTrace (use candidate directly instead of unpacking)
- [x] 5.5 Update History persistence mapping (`PersistenceMapping.cs`) for the new structure
- [x] 5.6 Update Scoring and History test projects, build and run all tests

## 6. MatchMetadata

- [x] 6.1 Create `MatchMetadata` record in `FunkArr.Messages/Shared/MatchMetadata.cs`
- [x] 6.2 Update `SearchResultItem` to embed `MatchMetadata?` replacing TvdbId/ImdbId/TmdbId/Season/Episode/MatchConfidence/MatchMethod fields (keep SubtitleUrl as separate field)
- [x] 6.3 Update `ReleaseVariant.ToResultItem()` to construct `MatchMetadata` from Identity and Match
- [x] 6.4 Update Newznab mapping (`NewznabSearchService`) to read from embedded MatchMetadata
- [x] 6.5 Update Search, ArrApi, and Api test projects, build and run all tests

## 7. Final verification

- [x] 7.1 Run full solution build (`dotnet build src/FunkArr.slnx`)
- [x] 7.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 7.3 Run all test projects (848 tests, 0 failures)
- [x] 7.4 Verify record parameter counts: WorkerStatusResult (12), QueueItem (12), MediathekItem (12) remain over 10 -- these are flat view/API records where further nesting would hurt readability. SearchResultItem reduced from 17 to 11. All other records are at 10 or below.
