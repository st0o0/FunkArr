## 1. DownloadManager Recovery Tests

- [x] 1.1 Create `DownloadManagerRecoveryTests.cs` with test fixture (probes for IDownloadRegion, IDownloadHistoryManager, TestOptionsMonitor, in-memory persistence config)
- [x] 1.2 Test: queue items survive restart (enqueue 3 items with concurrency=0, stop, recreate with concurrency=3, verify all 3 dispatched)
- [x] 1.3 Test: ResetDispatched on recovery (dispatch 2 items, restart, verify both re-dispatched)
- [x] 1.4 Test: priority order preserved after recovery (enqueue High/Normal/Low, restart, verify dispatch order)
- [x] 1.5 Test: pause state survives restart (pause, enqueue, restart, verify no dispatch)
- [x] 1.6 Test: snapshot restore produces correct state (persist 26 events to trigger snapshot, restart, verify all 26 dispatched)

## 2. DownloadWorker Journal Replay Tests

- [x] 2.1 Create `DownloadWorkerJournalReplayTests.cs` — state-level tests verifying event replay (DownloadWorker has too many DI deps for actor-level recovery tests)
- [x] 2.2 Test: full journal replay — initialized state
- [x] 2.3 Test: full journal replay — completed state
- [x] 2.4 Test: full journal replay — failed state
- [x] 2.5 Test: transient phase resets to initialized after recovery
- [x] 2.6 Test: completed phase not reset after recovery
- [x] 2.7 Test: multi-attempt replay tracks attempt count
- [x] 2.8 Test: failed state not transient stays failed

## 3. DownloadHistoryManager Recovery Tests

- [x] 3.1 Create `DownloadHistoryManagerRecoveryTests.cs` with in-memory persistence config
- [x] 3.2 Test: recorded entries survive restart (record 3 entries, restart, QueryHistory returns all 3)
- [x] 3.3 Test: removed entries stay removed after restart (record 2, remove 1, restart, verify only 1 remains)
- [x] 3.4 Test: stats correct after recovery (record mix of completed/failed, restart, verify stats match)
- [x] 3.5 Test: snapshot + event replay (record 27 entries triggering snapshot at 25, restart, verify all 27 present)

## 4. FeedCrawlManager Recovery Tests

- [x] 4.1 Create `FeedCrawlManagerRecoveryTests.cs` with probes, in-memory persistence, overwrite registry
- [x] 4.2 Test: cursor survives restart (drive completed crawl, restart, verify cursor and LastOutcome)
- [x] 4.3 Test: ConsecutiveFailures survives restart (trigger 2 failed harvests, restart, verify ConsecutiveFailures=2)
- [x] 4.4 Test: unchanged run persisted and survives restart (trigger unchanged, restart, verify LastOutcome=Unchanged)

## 5. FeedStoreManager Recovery Tests

- [x] 5.1 Create `FeedStoreManagerRecoveryTests.cs` with in-memory persistence config
- [x] 5.2 Test: entries survive restart (add 3 entries, restart, QueryFeed returns them)
- [x] 5.3 Test: expired entries stay expired after restart (add old+new, expire old, restart, verify only new remains)
- [x] 5.4 Test: deduplication works after restart (add entry, restart, re-add same, verify 0 published)

## 6. HistoryWorker Recovery Tests

- [x] 6.1 Create `HistoryWorkerRecoveryTests.cs` with in-memory persistence config
- [x] 6.2 Test: recorded entries survive restart (record 3 entries, restart, QueryScoringHistory returns all 3)
- [x] 6.3 Test: stats correct after replay (record entries, restart, QueryScoringStats matches)
- [x] 6.4 Test: OnReplaySuccess trims old entries (record 10 with MaxSnapshots=5, restart, verify trimmed to 5)
- [x] 6.5 Test: snapshot + events restored correctly (SnapshotInterval=5, record 7, restart, verify all 7 present)

## 7. Build and verify

- [x] 7.1 Run `dotnet build src/FunkArr.slnx` — 0 errors, 0 warnings
- [x] 7.2 Run Download.Tests — 339 total, 0 new failures (7 pre-existing from download-recovery-fixes)
- [x] 7.3 Run Feed.Tests — 85 total, 0 failures
- [x] 7.4 Run History.Tests — 160 total, 0 failures
