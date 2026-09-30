## 1. Fix _workerFailedReceived flag timing

- [x] 1.1 Move `_workerFailedReceived = true` from before the `Persist` call into the `Persist` callback in `HandleWorkerFailed` in `DownloadActor.cs`

## 2. Add OnPersistFailure and OnPersistRejected overrides

- [x] 2.1 Add `OnPersistFailure` override to `DownloadActor` that tells QueueActor `NotifyJobFinished(nzoId, "failed")` and DownloadRequestActor `FailDownload(nzoId, reason)`, then calls `base.OnPersistFailure`
- [x] 2.2 Add `OnPersistRejected` override with the same notification logic

## 3. Add per-stage ReceiveTimeout

- [x] 3.1 Add `ReceiveTimeout` handler to each stage behavior (Fetching, AcquiringSubtitle, ConvertingSubtitle, Muxing) that kills the current worker and calls `HandleWorkerFailed` with `FailureKind.Transient` and reason `"Stage timed out"`
- [x] 3.2 Set `Context.SetReceiveTimeout` on entry to each stage: Fetching 30min, AcquiringSubtitle 5min, ConvertingSubtitle 5min, Muxing 15min
- [x] 3.3 Clear ReceiveTimeout in Completed and WaitingForJob behaviors via `Context.SetReceiveTimeout(null)`

## 4. Fix OperationCanceledException gap in subtitle workers

- [x] 4.1 Remove `when (ex is not OperationCanceledException)` filter from `SubtitleDownloadActor.cs` catch clause
- [x] 4.2 Remove `when (ex is not OperationCanceledException)` filter from `SubtitleExtractActor.cs` catch clause

## 5. Tests

- [x] 5.1 Add test: DownloadActor transitions to Failed when Terminated arrives and no WorkerFailed was received
- [x] 5.2 Add test: DownloadActor ReceiveTimeout triggers failure transition after stage timeout
- [x] 5.3 Add test: ReceiveTimeout resets on ProgressTick (worker making progress does not time out)
- [x] 5.4 Verify existing tests still pass after all changes
