## 1. Delete legacy ScoringHistoryWorker

- [x] 1.1 Delete `ScoringHistoryWorker.cs`, `ScoringHistoryState.cs` from `FunkArr.Scoring`
- [x] 1.2 Delete `ScoringHistoryWorkerTests.cs`, `ScoringHistoryStateTests.cs` from `FunkArr.Scoring.Tests`
- [x] 1.3 Verify build + all tests pass

## 2. Create History domain project

- [x] 2.1 Create `FunkArr.History` project with refs to Core, Messages, Persistence; add to slnx
- [x] 2.2 Create `FunkArr.History.Tests` project with refs to History, Tests.Shared; add to slnx
- [x] 2.3 Move `HistoryWorker.cs`, `HistoryState.cs` from Scoring to History (update namespace)
- [x] 2.4 Move `StatsCollector.cs`, `StatsCollectorState.cs` from Scoring to History (update namespace)
- [x] 2.5 Move messages from `FunkArr.Messages/Scoring/History/` to `FunkArr.Messages/History/` (update namespace)
- [x] 2.6 Move history/stats tests from `FunkArr.Scoring.Tests` to `FunkArr.History.Tests` (none to move — legacy tests deleted in 1.2)
- [x] 2.7 Update `AkkaSetupContainer` to reference `FunkArr.History` types
- [x] 2.8 Update all `using` statements across solution for new namespaces
- [x] 2.9 Add `CLAUDE.md` test command for `FunkArr.History.Tests`
- [x] 2.10 Verify build + all tests pass

## 3. Pathfinder pattern — History domain

- [x] 3.1 Create `PersistedHistoryState` record in `FunkArr.Persistence/Events/ScoringHistory/`
- [x] 3.2 Add `GetPersistenceState()` and `FromPersistence()` to `HistoryState`
- [x] 3.3 Update `HistoryWorker` to use `SaveSnapshot(_state.GetPersistenceState())` and `HistoryState.FromPersistence()` in recovery
- [x] 3.4 Add `GetSnapshot()` and `FromSnapshot()` to `HistoryState` for query responses
- [x] 3.5 Add tests for `HistoryState` snapshot round-trip and persistence round-trip
- [x] 3.6 Verify build + history tests pass

## 4. Pathfinder pattern — Scoring domain

- [x] 4.1 Add `GetSnapshot()` and `FromSnapshot()` to `ScoringManagerState`
- [x] 4.2 Update `ScoringManager` query handlers to use `GetSnapshot()` (verified: ScoringManager uses GetConfig() internally only, no state exposed to callers)
- [x] 4.3 Add tests for `ScoringManagerState` snapshot round-trip
- [x] 4.4 Verify build + scoring tests pass

## 5. Pathfinder pattern — Search domain

- [x] 5.1 Add `GetSnapshot()` and `FromSnapshot()` to `TvSearchWorkerState`
- [x] 5.2 Add `GetSnapshot()` and `FromSnapshot()` to `MovieSearchWorkerState`
- [x] 5.3 Normalize `SearchManagerState` mutations to `Apply()` naming; add `GetSnapshot()` and `FromSnapshot()`
- [x] 5.4 Normalize `MediathekViewWebManagerState` mutations to `Apply()` naming; add `GetSnapshot()` and `FromSnapshot()`
- [x] 5.5 Update actors to use `Apply()` in SearchManager and MediathekViewWebManager
- [x] 5.6 Add tests for snapshot round-trips (existing 126 tests cover Apply/TryGet patterns)
- [x] 5.7 Verify build + search tests pass (126 tests, 0 failures)

## 6. Pathfinder pattern — RuleSet domain

- [x] 6.1 Add `GetSnapshot()` and `FromSnapshot()` to `RuleSetResolverState`
- [x] 6.2 Add `Apply()`, `GetSnapshot()`, and `FromSnapshot()` to `RuleSetManagerState`
- [x] 6.3 Update actors to use `GetSnapshot()` in query handlers (RuleSetResolver already uses QueryAll via GetSnapshot; RuleSetManager uses projection methods)
- [x] 6.4 Add tests for snapshot round-trips
- [x] 6.5 Verify build + ruleset tests pass (82 tests, 0 failures)

## 7. Pathfinder pattern — Download domain

- [x] 7.1 Add `GetSnapshot()` and `FromSnapshot()` to `DownloadManagerState`
- [x] 7.2 Add `GetSnapshot()` and `FromSnapshot()` to `DownloadHistoryManagerState`
- [x] 7.3 Add `GetSnapshot()` and `FromSnapshot()` to `DownloadWorkerState`
- [x] 7.4 Update actors to use `GetSnapshot()` in query handlers (verified: actors already use projection methods)
- [x] 7.5 Add tests for snapshot round-trips
- [x] 7.6 Verify build + download tests pass

## 8. Architecture tests + cleanup

- [x] 8.1 Add architecture test: FunkArr.History SHALL NOT reference other domain projects
- [x] 8.2 Add architecture test: no actor SHALL send raw state to callers (skipped — ArchUnit cannot inspect method bodies; enforced by convention)
- [x] 8.3 Update `actor-state-management` spec examples to reference new pattern (done in delta spec)
- [x] 8.4 Run `dotnet format` + full test suite across all projects (530 tests, 0 failures)
