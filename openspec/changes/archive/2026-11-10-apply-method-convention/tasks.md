## 1. DownloadWorkerState — wrapper records + rename

- [x] 1.1 Add `record ProgressUpdate(DownloadProgress Progress, DownloadPhase Phase, long TotalSize)` and `record PhaseChange(DownloadPhase Phase)` to `DownloadWorkerState.cs`
- [x] 1.2 Rename `ApplyProgress(...)` → `Apply(ProgressUpdate)` and `ApplyPhase(...)` → `Apply(PhaseChange)`
- [x] 1.3 Update caller in `DownloadWorker.cs` (lines 153, 167) to construct wrapper records
- [x] 1.4 Update tests in `DownloadWorkerStateTests.cs` and `DownloadWorkerRetryStateTests.cs`

## 2. DownloadManagerState — wrapper records + rename

- [x] 2.1 Add `record ConcurrencyLimit(int Limit)` and `record ScheduleChange(bool Enabled, DateTimeOffset? NextWindow)` to `DownloadManagerState.cs`
- [x] 2.2 Rename `ApplyMaxConcurrent(int)` → `Apply(ConcurrencyLimit)`, merge `ApplyScheduleEnabled()`/`ApplyScheduleDisabled(DateTimeOffset?)` → `Apply(ScheduleChange)`
- [x] 2.3 Update callers in `DownloadManager.cs` (lines 89, 365, 372)
- [x] 2.4 Update tests in `DownloadManagerScheduleGateTests.cs` and `DownloadManagerStateTests.cs` if they reference ApplySchedule*/ApplyMaxConcurrent

## 3. FeedCrawlPageActorState — wrapper records + rename

- [x] 3.1 Add `record FilterResult(int NewItems, int Unmatched, bool ReachedEnd, int GroupCount)` and `record EvalFailure` to `FeedCrawlPageActorState.cs` (EvalFailure to avoid collision with actor's private EvalFailed message)
- [x] 3.2 Rename `ApplyFilterResult(...)` → `Apply(FilterResult)`, `ApplyGateResult(GateResult)` → `Apply(GateResult)`, `ApplyEvalFailed()` → `Apply(EvalFailure)`
- [x] 3.3 Update callers in `FeedCrawlPageActor.cs` (lines 125, 194, 205)

## 4. FeedCrawlRunState — rename ApplyAll

- [x] 4.1 Rename `ApplyAll(IReadOnlyList<ProcessPageResponse>)` → `Apply(IReadOnlyList<ProcessPageResponse>)`
- [x] 4.2 Update caller in `FeedCrawlManager.cs` (line 308)

## 5. RuleSetManagerState — wrapper record + rename

- [x] 5.1 Add `record KnownRuleSetsUpdate(Dictionary<string, RuleSetPaths> RuleSets)` to `RuleSetManagerState.cs`
- [x] 5.2 Rename `ApplyKnownRuleSets(Dictionary)` → `Apply(KnownRuleSetsUpdate)`
- [x] 5.3 Update callers in `RuleSetManager.cs` (lines 70, 86, 155)

## 6. Documentation

- [x] 6.1 Update AGENTS.md "Actor state pattern" section (referenced from CLAUDE.md): add that primitive-param mutations get wrapper records, and all public mutation methods must be `Apply` overloads

## 7. Verify

- [x] 7.1 `dotnet build src/FunkArr.slnx` — 0 errors, 0 warnings
- [x] 7.2 `dotnet format src/FunkArr.slnx --verify-no-changes` — clean (pre-existing CRLF issue in SceneReleaseTests.cs unrelated)
- [x] 7.3 Run affected test projects — Download: 248 pass, Feed: 56 pass, RuleSet: 130 pass
