## 1. HIGH — Download Domain States

- [x] 1.1 `DownloadWorkerState`: Change all properties to `{ get; private set; }`. Add `Apply()` overloads for Progress, Phase, Status, Media mutations. Refactor `DownloadWorker` to call `Apply()` instead of direct property assignment.
- [x] 1.2 `DownloadManagerState`: Change `Paused`, `ScheduleEnabled`, `NextWindow`, `MaxConcurrent` to `{ get; private set; }`. Add `Apply()` methods. Refactor `DownloadManager` to call `Apply()`.

## 2. HIGH — Feed Domain States

- [x] 2.1 `FeedCrawlManagerState`: Move `Running` and `ResultReceived` to actor fields on `FeedCrawlManager`. Change remaining 8 properties to `{ get; private set; }`. Verify Apply methods cover all remaining properties. Refactor actor code.
- [x] 2.2 `FeedCrawlWorkerState`: Change 7 public-setter properties to `{ get; private set; }`. Add `Apply()` methods for `ListTimestamp`, `Lookup`, `Window` and other directly-set fields. Refactor `FeedCrawlWorker` to call `Apply()`.

## 3. HIGH — RuleSet Domain States

- [x] 3.1 `RuleSetWorkerState`: Change `Merged` and `Paths` to `{ get; private set; }`. Add `Apply()` methods (currently has none). Refactor `RuleSetWorker` to call `Apply()` instead of direct assignment.
- [x] 3.2 `RuleSetManagerState`: Change `KnownRuleSets` and `FullRescanRequested` to `{ get; private set; }`. Add `Apply()` methods for wholesale `KnownRuleSets` replacement and `FullRescanRequested` flag. Refactor `RuleSetManager`.

## 4. MEDIUM — Search Domain States

- [x] 4.1 `TvSearchWorkerState` and `MovieSearchWorkerState`: Change `Attempt` to `{ get; private set; }`. Add `Apply()` method for attempt increment. Refactor the workers.
- [x] 4.2 `MovieEvaluationState`: Change `RuleSetId`, `MediaName`, `ItemTraces` to `{ get; private set; }`. Verify no direct actor mutation (audit found none — confirm and fix if needed).

## 5. MEDIUM — Feed Scheduler and Remaining

- [x] 5.1 `FeedSchedulerState`: Change `Enabled` and `Interval` to `{ get; private set; }`. Verify Apply already covers all mutation (audit found it does — confirm).

## 6. Terp Consistency

- [x] 6.1 `ScoringManager`: Skipped — `ResolveChildActor` requires Hosting TestKit; tests use plain TestKit. Deferred to separate change.
- [x] 6.2 `FeedCrawlWorker`: Skipped — worker created without DI (`Props.Create(() => new ...)`), so child `ResolveChildActor` fails. Deferred to separate change.

## 7. Build and Test

- [x] 7.1 Build solution (`dotnet build src/FunkArr.slnx`) — fix compile errors from private setter enforcement.
- [x] 7.2 Run all test projects and fix failures.
- [x] 7.3 Run `dotnet format src/FunkArr.slnx --verify-no-changes` and fix any violations.
