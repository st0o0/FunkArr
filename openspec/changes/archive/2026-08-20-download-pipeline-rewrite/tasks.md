## 1. Pipeline models and cleanup infrastructure

- [x] 1.1 Remove `RetryCount` property from `DownloadJob.cs`
- [x] 1.2 Remove `MuxInput` record from `PipelineModels.cs` (unused)
- [x] 1.3 ~~CleanupStages~~ — not needed, `MuxingService.MuxAsync` already calls `CleanupTempFiles` on success
- [x] 1.4 Add `DownloadSourceDetector.PartitionOutlet` — returns outlet index (0 = HLS, 1 = MP4)

## 2. Stage factory updates

- [x] 2.1 Add `CancellationToken` parameter to `DownloadStages.Mp4Download` and propagate to `DownloadService.DownloadAsync`
- [x] 2.2 Add `CancellationToken` parameter to `DownloadStages.HlsDownload` and propagate to `HlsDownloadService.DownloadAsync`
- [x] 2.3 Add `CancellationToken` parameter to `SubtitleStages.Acquire` and propagate to `SubtitleAcquisitionService.AcquireAsync`
- [x] 2.4 Skipped — `SubtitleNormalizer.NormalizeAsync` is a fast local file op, no CT needed
- [x] 2.5 Add `CancellationToken` parameter to `MuxStages.Remux` and propagate to `MuxingService.MuxAsync`

## 3. GraphDSL pipeline composition

- [x] 3.1 Rewrite `MaterializeStream()` to use `GraphDSL.Create` with `Source.Queue`, KillSwitch, Partition (HLS/MP4), Merge, subtitle acquire, subtitle normalize, remux, and `Sink.ActorRef(self, new StreamCompleted())`
- [x] 3.2 Add `CancellationTokenSource` field to the actor, create in `MaterializeStream()`, cancel + dispose in `PostStop()` and before re-materialization
- [x] 3.3 Remove the monolithic `SelectAsyncUnordered` lambda — stages now wired via GraphDSL
- [x] 3.4 Remove `Sink.ForEach` callback and `done.ContinueWith().PipeTo()` — `Sink.ActorRef` handles completion, `Status.Failure` handles errors

## 4. Actor message handling updates

- [x] 4.1 Add `Command<MuxOutcome.Success>` handler — persists `MuxingCompleted`, records metrics
- [x] 4.2 Add `Command<MuxOutcome.Failure>` handler — persists `MuxingFailed`, records metrics
- [x] 4.3 Add `Command<MuxOutcome.Skipped>` handler (no-op)
- [x] 4.4 Removed `HandleMuxingCompleted`/`HandleMuxingFailed` — replaced by `HandleMuxSuccess`/`HandleMuxFailure`

## 5. Recovery fix

- [x] 5.1 Add `PushQueuedJobs()` call in `OnRecoveryCompleted()` after `Materializing()`

## 6. Tests

- [x] 6.1 N/A — no existing DownloadQueueActor unit tests; integration tests pass
- [x] 6.2 Build succeeds with 0 warnings, 0 errors
- [x] 6.3 Full test suite: 443 tests, 0 failures
