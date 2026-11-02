## 1. Scaffold New Actors and State

- [x] 1.1 Create `FeedScheduler.cs` — `ReceiveActor, IWithTimers`, reads `FeedOptions.Interval`/`Enabled` via `IOptionsMonitor`, private `Tick` timer message, sends `StartFeedCrawl` to `Context.GetActor<IFeedCrawlManager>()`
- [x] 1.2 Create `FeedSchedulerState.cs` — Pathfinder record with `Empty`, tracks `Enabled`/`Interval`, extension methods `Apply(FeedOptions)`
- [x] 1.3 Add `IFeedScheduler` marker interface to `ActorKeys.cs`
- [x] 1.4 Register `FeedScheduler` as singleton in `AkkaSetupContainer`, register in actor registry

## 2. FeedPageActor (Pool Worker)

- [x] 2.1 Create `FeedPageActor.cs` — `ReceiveActor`, resolves `IMediathekManager` via `Context.GetActor<T>()`, receives `FetchPage(Offset, Window)`, Asks MvwManager with `PipeTo(Self)`, window-filters items, returns `PageResult(Offset, Items, ReachedEnd, Error)` to `Sender`
- [x] 2.2 Create `FeedPageMessages.cs` — `FetchPage` record and `PageResult` record (internal, Feed-only messages)
- [x] 2.3 Add FeedPageActor unit tests — successful fetch, empty page (ReachedEnd), MvwManager error, MvwManager queue full, window filtering (items outside window excluded)

## 3. FeedEvalActor (Pool Worker)

- [x] 3.1 Create `FeedEvalActor.cs` — `ReceiveActor`, resolves `ITvEvaluationRegion`/`IMovieEvaluationRegion` via `Context.GetActor<T>()`, receives `EvalGroup(RuleSetId, MediaType, Ids, Items)`, Asks TvEval or MovieEval with `PipeTo(Self)`, runs `FeedGate.Evaluate`, records `Telemetry.Rejected` at actor boundary, returns `GroupResult(RuleSetId, Accepted, Error)` to `Sender`
- [x] 3.2 Create `FeedEvalMessages.cs` — `EvalGroup` record and `GroupResult` record (internal, Feed-only messages)
- [x] 3.3 Add FeedEvalActor unit tests — successful TV evaluation, successful movie evaluation, evaluation failure, gate rejects low-confidence, telemetry recorded for rejections

## 4. FeedCrawlWorker (Become-Phase Child Actor)

- [x] 4.1 Create `FeedCrawlWorkerState.cs` — Pathfinder record: `Empty` static field, progressive enrichment via extension methods `Apply(ProbeResult)`, `Apply(RuleSetsLoaded)`, `Apply(PageResult)`, `Apply(GroupResult)`. Fields: `CrawlInput`, `ListTimestamp`, `CrawlWindow`, `RuleSetLookup`, `CollectedItems`, `PendingPages`, `NextOffset`, `ReachedEnd`, `PageError`, `MatchedGroups`, `PendingGroups`, `CollectedResults`. `GetCrawlResult()` extension builds final `CrawlResult`
- [x] 4.2 Create `FeedCrawlWorker.cs` — `ReceiveActor`, resolves deps via `Context.GetActor<T>()`, parent via `Context.Parent`. Creates two child pools in constructor: `pageFetchPool` (`SmallestMailboxPool(input.PagePrefetch)` of `FeedPageActor`) and `evalPool` (`SmallestMailboxPool(input.MaxConcurrency)` of `FeedEvalActor`)
- [x] 4.3 Implement Phase 1 Probing — Ask MvwManager for list timestamp via PipeTo. Handle ProbeResult: unchanged → `CrawlResult.ForUnchanged` → Complete. Handle ProbeFailed → Complete with error
- [x] 4.4 Implement Phase 2 LoadingRuleSets — Ask RuleSetResolver via PipeTo. Handle RuleSetsLoaded: build `RuleSetLookup`, compute `CrawlWindow`, Become(Harvesting). Handle failure → Complete with error
- [x] 4.5 Implement Phase 3 Harvesting — Fire N initial `FetchPage` to pageFetchPool (N = pool size). Handle `PageResult`: apply to state, if not reachedEnd fire next page. When `PendingPages == 0` → Become(Evaluating)
- [x] 4.6 Implement Phase 4 Evaluating — Sync: dedup items (HashSet on StableId), match via `RuleSetLookup.Match`, group by RuleSetId. Fire all `EvalGroup` messages to evalPool. Handle `GroupResult`: apply to state, decrement pending. When `PendingGroups == 0` → Become(Publishing)
- [x] 4.7 Implement Phase 5 Publishing — Collect all accepted entries from GroupResults. Batch Ask `FeedStoreManager` via PipeTo. Handle result → Become(Complete)
- [x] 4.8 Implement Phase 6 Complete — Tell `Context.Parent` the `CrawlResult`, `Context.Stop(Self)`
- [x] 4.9 Add FeedCrawlWorkerState unit tests — `Empty`, progressive `Apply` for each phase, `GetCrawlResult` for completed/failed/unchanged

## 5. Rewrite FeedCrawlManager

- [x] 5.1 Remove all timer logic from FeedCrawlManager (IWithTimers, Timers.StartSingleTimer, Tick handling) — timer moves to FeedScheduler
- [x] 5.2 Remove all stream-related code and usings (IMaterializer, Akka.Streams.*)
- [x] 5.3 Rewrite StartCrawl: build `CrawlInput` from options + state, spawn `FeedCrawlWorker` via `resolver.Props<FeedCrawlWorker>()`, `Context.Watch`, set `_running`
- [x] 5.4 Handle `CrawlResult` from worker: persist cursor advance, update telemetry, reset `_running`
- [x] 5.5 Handle `Terminated`: if CrawlResult not yet received, treat as failed crawl
- [x] 5.6 Update FeedCrawlManagerTests for new shape — verify spawns worker, handles CrawlResult, handles Terminated, no more timer tests (moved to FeedScheduler)

## 6. Cleanup and Type Reorganization

- [x] 6.1 Delete `FeedCrawler.cs`, `FeedCrawlerState.cs`, `FeedCrawlerTypes.cs`
- [x] 6.2 Move shared crawl types (`CrawlAccumulator` or equivalent) into `FeedCrawlWorkerState.cs` or remove if no longer needed
- [x] 6.3 Fix `PersistenceMapping.cs` visibility — `internal static class` → `public static class` (or methods → `internal`)
- [x] 6.4 Remove `EvalResult.Gate()` telemetry side-effect — telemetry now in FeedEvalActor
- [x] 6.5 Update `FeedGate.cs` if needed — ensure `Evaluate` is a pure static method returning rejection counts alongside accepted entries
- [x] 6.6 Remove Akka.Streams NuGet reference from FunkArr.Feed.csproj if no longer needed

## 7. Integration and Verification

- [x] 7.1 Delete `FeedCrawlerStateTests.cs`, add new `FeedCrawlWorkerStateTests.cs`
- [x] 7.2 Add FeedScheduler tests — timer fires StartCrawl at interval, respects Enabled flag, reacts to option changes
- [x] 7.3 Update integration tests if any reference deleted types
- [x] 7.4 Verify all existing FeedStoreManager tests still pass (unchanged)
- [x] 7.5 Run full build (`dotnet build src/FunkArr.slnx`)
- [x] 7.6 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 7.7 Run all Feed tests (`dotnet run --project src/FunkArr.Feed.Tests`)
