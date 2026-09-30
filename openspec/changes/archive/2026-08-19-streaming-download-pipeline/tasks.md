## 1. Infrastructure & Types

- [x] 1.1 Add `MuxConcurrency` (default 4) to `FunkArrOptions`, change `ConcurrentDownloads` default to 20
- [x] 1.2 Create `DownloadOutcome` discriminated union: `Success(NzoId, VideoPath, SubtitlePath?)` and `Failure(NzoId, Reason)` in `DownloadClient/`
- [x] 1.3 Create `MuxOutcome` discriminated union: `Success(NzoId, OutputPath)`, `Failure(NzoId, Reason)`, and `Skipped(NzoId, Reason)` in `Muxing/`
- [x] 1.4 Create `MatchContext` record: `ShowName?`, `Season?`, `Episode?`, `AirDate?`, `ExpectedDurationSeconds?`, `ImdbId?` in `Search/`
- [x] 1.5 Create `StreamSupervision` static class with `LoggingDecider(ILoggingAdapter)` in `Shared/` — Resume on `TaskCanceledException`/`OperationCanceledException`, Stop on everything else

## 2. Extract MuxingService

- [x] 2.1 Create `MuxingService` class in `Muxing/` — extract `MuxAsync(videoPath, subtitlePath, outputDir, title, cancellationToken)` from MuxingActor, returning `MuxOutcome`
- [x] 2.2 Move `NormalizeSubtitleAsync`, `BuildFfmpegArgs`, `ConvertVttToSrt`, `ConvertTtmlToSrt`, `CleanupTempFiles` to `MuxingService`
- [x] 2.3 Register `MuxingService` in `FunkArrServiceSetup` DI container
- [x] 2.4 Write tests for `MuxingService` — FFmpeg argument building, subtitle normalization, VTT/TTML conversion

## 3. Rewrite DownloadQueueActor with Akka.Streams

- [x] 3.1 Add `IWithStash` to DownloadQueueActor, implement `Recovering`/`Materializing`/`Ready` become states
- [x] 3.2 Create `DownloadRequest` record for stream elements: `NzoId`, `VideoUrl`, `SubtitleUrl?`, `TempPath`, `OutputDir`, `Title`
- [x] 3.3 Implement stream graph: `Source.Queue<DownloadRequest>(64, Backpressure)` → `SelectAsyncUnordered(ConcurrentDownloads, downloadLambda)` → `SelectAsyncUnordered(MuxConcurrency, muxLambda)` → `Sink.ForEach(Self.Tell)`
- [x] 3.4 Implement download lambda — HTTP stream download with progress reporting via `Self.Tell`, typed `DownloadOutcome` return, catches all exceptions
- [x] 3.5 Implement mux lambda — calls `MuxingService.MuxAsync()`, returns `MuxOutcome`, skips on `DownloadOutcome.Failure` input
- [x] 3.6 Apply `StreamSupervision.LoggingDecider` and `SharedKillSwitch` to the graph
- [x] 3.7 Handle stream completion/failure messages — re-materialize on unexpected termination
- [x] 3.8 Wire `OfferAsync` for enqueue in `Ready` state, pipe result to Self for backpressure awareness
- [x] 3.9 Update recovery logic — after replaying events, transition to `Materializing`, then push all `Queued` jobs into Source.Queue
- [x] 3.10 Update persistence event handlers to work with stream lifecycle messages (`CompletedInternal`, `FailedInternal`, `MuxingStartedInternal`, etc.)

## 4. Remove Old Actors

- [x] 4.1 Delete `DownloadWorkerActor.cs`
- [x] 4.2 Delete `MuxingActor.cs` (logic lives in `MuxingService` now)
- [x] 4.3 Remove worker/muxing actor registration from `FunkArrActorSystemSetup`
- [x] 4.4 Remove worker-related fields from DownloadQueueActor (`_activeWorkers`, `_workerCounter`, `StartWorker`, `TryStartNextDownload`)

## 5. Compose Matching Pipeline

- [x] 5.1 Add `MatchesShow(MediathekResultItem, MatchContext)` — fuzzy match against Topic using `NormalizeTitle`, prefix matching
- [x] 5.2 Add `MatchesEpisode(MediathekResultItem, MatchContext)` — S##E## extraction, air date matching via `DateMatcher`, skip filter when no season/episode/date in context
- [x] 5.3 Add `ScoreResult(SearchResult, MatchContext)` — weighted scoring: title closeness + date proximity + quality tier
- [x] 5.4 Compose `MatchingPipeline.Execute(items, context)` as LINQ chain: `Where(NotJunk) → Where(MatchesShow) → Where(MatchesEpisode) → Where(DurationOk) → SelectMany(ExpandQualities) → Select(Score) → OrderByDescending(Score)`
- [x] 5.5 Update `SearchActor` to construct `MatchContext` from request parameters and call `MatchingPipeline.Execute()` instead of `FilterResults()`
- [x] 5.6 Write tests for matching pipeline — junk filter, show matching, episode matching, scoring, full pipeline composition

## 6. Wire TVDB Lookup

- [x] 6.1 Wire `TvdbClient` in `SearchActor.HandleTvSearch` — resolve TvdbId to German show name when `ShowName` is null
- [x] 6.2 Retrieve episode air date from TVDB for season/episode and include in `MatchContext.AirDate`
- [x] 6.3 Write tests for TVDB fallback — API failure falls back to `q` parameter

## 7. Wire API Endpoints

- [x] 7.1 Wire `NewznabEndpoints.HandleTvSearch` — construct `TvSearchRequest`, ask `SearchActor`, convert `SearchResult[]` to Newznab XML via `NewznabXmlBuilder`
- [x] 7.2 Wire `NewznabEndpoints.HandleMovieSearch` — construct `MovieSearchRequest`, ask `SearchActor`, convert to Newznab XML
- [x] 7.3 Wire `NewznabEndpoints.HandleTextSearch` — construct `TextSearchRequest`, ask `SearchActor`, convert to Newznab XML
- [x] 7.4 Wire `SabnzbdEndpoints` addfile — parse fake NZB, send `EnqueueDownload` to `DownloadQueueActor`, return `nzo_ids` JSON
- [x] 7.5 Wire `SabnzbdEndpoints` queue — ask `DownloadQueueActor` for active jobs, format as SABnzbd queue JSON
- [x] 7.6 Wire `SabnzbdEndpoints` history — ask `DownloadQueueActor` for completed/failed jobs, format as SABnzbd history JSON

## 8. Integration Tests

- [x] 8.1 Test DownloadQueueActor stream lifecycle — materialize, enqueue, complete, fail, re-materialize after crash
- [x] 8.2 Test DownloadQueueActor persistence recovery — persist events, restart actor, verify jobs re-queued and stream re-materialized
- [x] 8.3 Test StreamSupervision decider — verify Resume/Stop decisions for each exception type
- [x] 8.4 Test end-to-end Newznab search → SearchActor → MatchingPipeline response
- [x] 8.5 Test end-to-end SABnzbd addfile → DownloadQueueActor enqueue
