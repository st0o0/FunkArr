## 1. MVW Queue Saturation Tests

- [x] 1.1 Create `MediathekViewWebManagerTests.cs` in Search.Tests with TestKit
- [x] 1.2 Test: QueryMediathek receives `QueryMediathekCompleted` on success
- [x] 1.3 Test: Queue full sends `QueryMediathekQueueFull` when stream drops new
- [x] 1.4 Test: StreamFailure sends `QueryMediathekError` to correct sender

## 2. RuleSet Updater Degraded Mode Tests

- [x] 2.1 Create `RuleSetUpdaterDegradedTests.cs` in RuleSet.Tests
- [x] 2.2 Test: GitHub 500 → logs warning, schedules next check
- [x] 2.3 Test: GitHub 403 → same behavior as 500
- [x] 2.4 Test: GitHub timeout (TaskCanceledException) → same behavior

## 3. FFmpeg Crash Tests

- [x] 3.1 Create `DownloadWorkerFfmpegCrashTests.cs` in Download.Tests/Worker
- [x] 3.2 Test: Permanent failure after init → status Failed, correct message
- [x] 3.3 Test: Transient failure → status Failed, retry possible
- [x] 3.4 Test: Multiple failures exhaust retries → status Failed, no more retries

## 4. Build & Verify

- [x] 4.1 `dotnet build src/FunkArr.slnx` — zero errors
- [x] 4.2 Run Search.Tests, RuleSet.Tests, Download.Tests — all pass
