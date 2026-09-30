## 1. File renames and persistence cleanup

- [x] 1.1 Rename `DownloadCoordinatorTests.cs` → `DownloadActorTests.cs` and update class name
- [x] 1.2 Rename `DownloadRequestTrackerTests.cs` → `DownloadRequestActorTests.cs` and update class name
- [x] 1.3 Replace inline HOCON persistence config with `builder.AddTestPersistence()` in all actor test files (`DownloadActorTests`, `DownloadRequestActorTests`, `MovieActorTests`, `ShowActorTests`)

## 2. Remove delays — Pattern A (synchronous Tell → Ask)

- [x] 2.1 `MovieActorTests`: Remove all 5 `Task.Delay(200)` calls — mailbox ordering guarantees Ask sees Tell result
- [x] 2.2 `ShowActorTests`: Remove all 5 `Task.Delay(200)` calls — same pattern as MovieActorTests
- [x] 2.3 `DownloadRequestActorTests`: Remove all 11 `Task.Delay(100)` calls — in-memory Persist completes synchronously with stashing

## 3. Remove delays — Pattern B + C (bulk Tell and init waits)

- [x] 3.1 `RecentMatchActorTests`: Remove 6 init `Task.Delay(300)` calls after actor creation — recovery is instant with empty in-memory journal
- [x] 3.2 `RecentMatchActorTests`: Replace 5 post-Tell `Task.Delay(500/2000)` with `AwaitAssertAsync` retry pattern for bulk message processing
- [x] 3.3 `RuleSetRegistryActorTests`: Remove 3 `Task.Delay(500)` init waits — first `Ask` serves as implicit init barrier

## 4. Remove delays — Pattern D (state machine with probes)

- [x] 4.1 `DownloadActorTests`: Replace all 11 `Task.Delay(200)` with `_trackerProbe.ExpectMsg<ReportProgress>()` as deterministic state-transition barrier

## 5. Verification

- [x] 5.1 Run full test suite and verify all tests pass
- [x] 5.2 Run actor tests multiple times to confirm no flakiness
