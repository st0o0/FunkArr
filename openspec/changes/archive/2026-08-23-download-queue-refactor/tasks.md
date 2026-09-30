## 1. Progress infrastructure

- [x] 1.1 Create `DownloadProgress` class with `Volatile.Read`/`Volatile.Write` fields (`DownloadedBytes`, `TotalBytes`)
- [x] 1.2 Add `IProgress<DownloadProgress>` property to `DownloadRequest`
- [x] 1.3 Change `DownloadService.DownloadAsync` from `Action<long, long> onProgress` to `IProgress<DownloadProgress> progress`
- [x] 1.4 Change `HlsDownloadService.DownloadAsync` from `Action<long, long> onProgress` to `IProgress<DownloadProgress> progress`
- [x] 1.5 Remove `DownloadProgressUpdated` from `DownloadEvents`
- [x] 1.6 Remove `ProgressPercent`, `DownloadedBytes`, `TotalBytes` from `DownloadJob`

## 2. Stream pipeline models

- [x] 2.1 Add `OutputDir` and `Title` fields to `VideoDownloadResult`
- [x] 2.2 Add `OutputDir` and `Title` fields to `SubtitleResult`
- [x] 2.3 Add `OutputDir` and `Title` fields to `NormalizedResult`
- [x] 2.4 Thread `OutputDir`/`Title` from `DownloadRequest` through `DownloadStages` into `VideoDownloadResult`
- [x] 2.5 Thread `OutputDir`/`Title` through `SubtitleStages.Acquire` and `SubtitleStages.Normalize`
- [x] 2.6 Update `MuxStages.Remux` to read `OutputDir`/`Title` from `NormalizedResult` and remove `LookupJob` parameter

## 3. Unified download flow

- [x] 3.1 Create `DownloadStages.Download()` returning `Flow<DownloadRequest, VideoDownloadResult, NotUsed>` using `Flow.FromGraph(GraphDsl.Create(...))` with internal Partition/Merge
- [x] 3.2 Update `DownloadStages.Mp4Download` and `HlsDownload` to use `IProgress<DownloadProgress>` from `DownloadRequest` and `IActorRef parent` instead of `self`
- [x] 3.3 Remove progress `self.Tell(DownloadProgressUpdated)` calls from both download stages

## 4. Extract DownloadQueueState

- [x] 4.1 Create `DownloadQueueState` class with internal `Dictionary<string, DownloadJob>` and `Apply` methods for all 7 domain events
- [x] 4.2 Add `ActiveJobs` and `History` query methods
- [x] 4.3 Add `ResetInFlight()` method
- [x] 4.4 Add `TryGetJob(string nzoId)` lookup method
- [x] 4.5 Write unit tests for `DownloadQueueState` — event application, queries, reset logic

## 5. Create DownloadPipelineActor

- [x] 5.1 Create `DownloadPipelineActor` as `ReceiveActor` with `IServiceProvider` and `DownloadOptions` constructor parameters
- [x] 5.2 Implement `MaterializeStream()` with linear `.Via()` chain using `DownloadStages.Download()`, `SubtitleStages`, `MuxStages`, and `Sink.ActorRef(Context.Parent)`
- [x] 5.3 Implement `OfferDownload` message handling with `queue.OfferAsync` and result piping
- [x] 5.4 Implement `StreamCompleted` and `Status.Failure` handling with re-materialization and parent notification
- [x] 5.5 Implement `PostStop` cleanup (CTS cancel, KillSwitch shutdown, queue complete)

## 6. Refactor DownloadQueueActor

- [x] 6.1 Replace 6 service dependencies with `IServiceProvider` and `IOptions<DownloadOptions>`
- [x] 6.2 Replace all `ApplyEvent` methods with delegation to `DownloadQueueState`
- [x] 6.3 Replace `HandleGetQueue`/`HandleGetHistory` with delegation to state query methods
- [x] 6.4 Replace `ResetInFlightJobs`/`PushQueuedJobs` with state delegation and child actor messaging
- [x] 6.5 Remove all stream-related code (`MaterializeStream`, `_materializer`, `_queue`, `_killSwitch`, `_cts`)
- [x] 6.6 Remove `HandleProgressUpdate` and `DownloadProgressUpdated` command handler
- [x] 6.7 Remove `LookupJob` method
- [x] 6.8 Create pipeline child actor in `OnRecoveryCompleted`
- [x] 6.9 Update `HandleEnqueue` to create `DownloadProgress` object, build `DownloadRequest` with it, and send `OfferDownload` to child
- [x] 6.10 Update `QueueResponse` to include `DownloadProgress` references alongside jobs

## 7. Update controllers and actor setup

- [x] 7.1 Update `QueueController` to merge progress data from `DownloadProgress` objects into response
- [x] 7.2 Update `SabnzbdController` to read progress from `DownloadProgress` objects
- [x] 7.3 Update `FunkArrActorSystemSetup` for new actor constructor signature

## 8. Tests and verification

- [x] 8.1 Update existing `DownloadServiceTests` for `IProgress<DownloadProgress>` signature
- [x] 8.2 Update existing integration tests that reference progress fields or DownloadJob shape
- [x] 8.3 Verify all existing tests pass
- [x] 8.4 Run `dotnet format` and verify build
