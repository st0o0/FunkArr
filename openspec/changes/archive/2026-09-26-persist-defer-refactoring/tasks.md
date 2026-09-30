## 1. DownloadManager - DeferAsync + SaveSnapshot

- [x] 1.1 Add SnapshotInterval constant, MaybeSnapshot, SnapshotOffer recovery, SaveSnapshotSuccess/Failure handlers
- [x] 1.2 HandleAdd - kept inline (DispatchNext calls PersistAll, incompatible with DeferAsync)
- [x] 1.3 HandleSlotFree - kept inline (same reason: DispatchNext)
- [x] 1.4 HandleDelete - DeferAsync for Tell + Sender.Tell (no DispatchNext)
- [x] 1.5 HandleRetry - kept inline (DispatchNext)
- [x] 1.6 HandleResume - kept inline (DispatchNext)
- [x] 1.7 HandleForceStart - DeferAsync for Tell + Sender.Tell (no DispatchNext)
- [x] 1.8 HandleMove - DeferAsync for Sender.Tell in nested Persist path
- [x] 1.9 HandleSetPriority - kept inline (DispatchNext)
- [x] 1.10 Replace ContinueWith with async helper method in fan-out pattern
- [x] 1.11 DispatchNext PersistAll - kept inline (DispatchNext is called from Persist callback)
- [x] 1.12 Created PersistedDownloadManagerState, GetPersistenceState/FromPersistence

## 2. DownloadWorker - DeferAsync

- [x] 2.1 HandleStart (empty URL) - DeferAsync for Tell to 2 actors
- [x] 2.2 HandleStart (normal URL) - DeferAsync for StartFfmpeg
- [x] 2.3 HandleFfmpegResult (success) - DeferAsync for file cleanup + Tell to 2 actors
- [x] 2.4 HandleFfmpegResult (failure) - DeferAsync for Tell to 2 actors

## 3. DownloadHistoryManager - SaveSnapshot

- [x] 3.1 Add SnapshotInterval, MaybeSnapshot, SnapshotOffer recovery, SaveSnapshotSuccess/Failure handlers
- [x] 3.2 Created PersistedDownloadHistoryManagerState, GetPersistenceState/FromPersistence

## 4. TimeProvider

- [x] 4.1 Inject TimeProvider into TvdbClient, replace DateTime.UtcNow
- [x] 4.2 Add TimeProvider parameter to NzbService.GetNzb, replace DateTime.UtcNow
- [x] 4.3 Update NewznabController, test setups (NzbServiceTests, TvdbClientTests, EnrichmentManagerTests, FunkArrTestServer)

## 5. Verification

- [x] 5.1 dotnet build - 0 errors
- [x] 5.2 dotnet format - no new violations (pre-existing IDE1006 and ENDOFLINE only)
- [x] 5.3 All test projects pass: Download (199), ArrApi (113), Search (129), RuleSet (128), Scoring (65), History (41), Enrichment (58), Api (54), Architecture (12) = 799 total, 0 failed
