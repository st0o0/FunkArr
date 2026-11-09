## 1. Internal messages and state types

- [x] 1.1 Create FeedCrawlPageMessages.cs with ProcessPage and PageProcessed records in FunkArr.Feed.Crawl namespace
- [x] 1.2 Create FeedCrawlRunState.cs — transient crawl session state (pending pages, next offset, reached-end, collected entries, stats, window, lookup, error tracking). Include Apply(PageProcessed), NextPageRequest(), HasMorePages, GetCrawlResult() methods
- [x] 1.3 Add scheduling fields to FeedCrawlManagerState — absorb interval/enabled from FeedSchedulerState as transient (config-derived) fields. Add Apply(FeedOptions) method for config changes

## 2. FeedCrawlPageActor

- [x] 2.1 Create FeedCrawlPageActor.cs — pool routee with Become states (Ready → Fetching → Evaluating → Ready). Resolve IMediathekManager, ITvEvaluationRegion, IMovieEvaluationRegion via Context.GetActor<T>(). Private sealed records for PipeTo wrappers (FetchCompleted, FetchFailed, EvalCompleted, EvalFailed)
- [x] 2.2 Implement Fetching phase — Ask MediathekManager, window filter, dedup by StableId, group by RuleSetLookup. On fetch failure: reply PageProcessed with error, Become(Ready)
- [x] 2.3 Implement Evaluating phase — fire N eval Asks (TvEval/MovieEval per group), countdown pending, apply FeedGate.Evaluate per completed group, aggregate accepted entries and rejection stats. On all groups done: reply PageProcessed, Become(Ready)

## 3. FeedCrawlManager rewrite

- [x] 3.1 Add IWithTimers to FeedCrawlManager. Create page-pool as permanent child (SmallestMailboxPool × MaxConcurrency) after recovery. Add timer scheduling with startup delay and OptionsChanged handler
- [x] 3.2 Implement Idle state — timer tick triggers probe (if enabled), QueryFeedCrawlStatus handler, StartFeedCrawl handler
- [x] 3.3 Implement Probing state — Ask MediathekManager for listTimestamp via PipeTo, handle ProbeResult/ProbeFailed, transition to LoadingRuleSets or back to Idle
- [x] 3.4 Implement LoadingRuleSets state — Ask RuleSetResolver via PipeTo, create FeedCrawlRunState with window and lookup, transition to Crawling
- [x] 3.5 Implement Crawling state — fire initial pages into pool, handle PageProcessed (delegate to RunState.Apply, fire next page or transition to Publishing/Idle)
- [x] 3.6 Implement Publishing state — Ask FeedStoreManager with PublishFeedEntries, persist cursor advance or failure, telemetry recording, transition to Idle
- [x] 3.7 Update FeedCrawlManagerState.ToStatus() to include scheduling info (interval, enabled) for QueryFeedCrawlStatus responses

## 4. Remove eliminated actors

- [x] 4.1 Delete FeedCrawlWorker.cs and FeedCrawlWorkerState.cs
- [x] 4.2 Delete FeedPageActor.cs and FeedPageMessages.cs
- [x] 4.3 Delete FeedEvalActor.cs and FeedEvalMessages.cs
- [x] 4.4 Delete FeedScheduler.cs and FeedSchedulerState.cs
- [x] 4.5 Update AkkaSetupContainer — remove FeedScheduler singleton registration (IFeedScheduler), remove IFeedScheduler interface if it exists
- [x] 4.6 Update FeedSetupContainer or service registration — remove FeedScheduler-related DI registrations
- [x] 4.7 Delete IFeedScheduler marker interface from FunkArr.Core if it exists

## 5. Tests

- [ ] 5.1 Create FeedCrawlRunStateTests.cs — unit tests for Apply(PageProcessed), NextPageRequest(), HasMorePages, GetCrawlResult(), aggregation of stats across pages, error tracking
- [ ] 5.2 Create FeedCrawlPageActorTests.cs — TestKit tests with TestProbes for MediathekManager, TvEvaluationRegion, MovieEvaluationRegion. Test: successful page pipeline, fetch failure, eval failure, no matches, Become transitions
- [ ] 5.3 Rewrite FeedCrawlManagerTests.cs — TestKit tests with TestProbe page-pool. Test: Idle→Probing→LoadingRuleSets→Crawling→Publishing→Idle flow, timer scheduling, status queries in each state, partial harvest, failure tracking, cursor persistence
- [x] 5.4 Delete FeedCrawlWorkerStateTests.cs (replaced by FeedCrawlRunStateTests)
- [ ] 5.5 Verify FeedStoreManagerTests, FeedGateTests, RuleSetLookupTests, CrawlResultTests still pass unchanged

## 6. Cleanup and verification

- [x] 6.1 Run dotnet build and dotnet format — fix any compilation errors from removed types
- [ ] 6.2 Run all Feed.Tests — verify all tests pass
- [ ] 6.3 Update AGENTS.md actor topology section — reflect new 3-actor / 2-level Feed topology
- [ ] 6.4 Verify architecture tests still pass (FunkArr.Architecture.Tests)
