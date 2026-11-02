## Why

The Feed domain was recently refactored (feed-stream-redesign) to extract a FeedCrawler child actor from FeedCrawlManager. While the refactor improved separation, it introduced multiple convention violations: the actor takes 6 IActorRef constructor params instead of using registry resolution, the state record uses instance methods instead of the Pathfinder extension pattern, the stream still captures 3 mutable closure variables, and telemetry side-effects live inside record methods. Rather than patching these one by one, we rewrite the Feed crawl pipeline from scratch using pure actor patterns — consistent with how Download, Search, Scoring, and Enrichment are built.

## What Changes

- **Delete** `FeedCrawler.cs`, `FeedCrawlerState.cs`, `FeedCrawlerTypes.cs` — replace entirely
- **New** `FeedScheduler` — timer-only singleton (like DownloadScheduler), extracted from FeedCrawlManager
- **New** `FeedCrawlWorker` — Become-phase child actor for one crawl lifecycle (Probing → LoadingRuleSets → Harvesting → Processing → Publishing → Complete)
- **New** `FeedPageActor` — pool worker for parallel page fetching via MvwManager
- **New** `FeedEvalActor` — pool worker for parallel evaluation+gating via TvEval/MovieEval
- **Rewrite** `FeedCrawlManager` — thin orchestrator: cursor persistence, spawn/watch worker, handle result. No timer logic, no stream code
- **Remove** all Akka.Streams usage from Feed domain — no more `Source`, `Flow`, `Sink`, `IMaterializer`
- **Replace** stream pipeline with actor Become-phases: page fetching via SmallestMailboxPool(3), evaluation via SmallestMailboxPool(N), scatter-gather pattern for collecting results
- All actors use `Context.GetActor<T>()` and `Context.Parent` — zero IActorRef constructor params
- All state records follow Pathfinder pattern: Empty field, extension methods, public visibility
- Telemetry recording at actor boundaries, not inside record methods

## Capabilities

### New Capabilities
- `feed-actor-topology`: Actor hierarchy, lifecycle, pool configuration, and phase transitions for the rewritten Feed crawl pipeline
- `feed-page-fetch`: Parallel page fetching via actor pool with sliding window, window filtering, and ReachedEnd detection
- `feed-eval-gate`: Parallel evaluation and gating via actor pool with scatter-gather result collection

### Modified Capabilities
- `feed-crawl-lifecycle`: Crawl phases change from Probe→RuleSets→Stream to Probe→RuleSets→Harvest→Process→Publish with actor pools instead of stream stages
- `crawl-stream-pure`: **REMOVED** — stream pipeline replaced entirely by actor phases and pools
- `feed-domain-model`: FeedEntry record unchanged, but PersistenceMapping visibility fixed (internal class with public methods → consistent visibility)

## Impact

- **FunkArr.Feed**: All crawl-related files rewritten. FeedStoreManager unchanged. New files: FeedScheduler, FeedCrawlWorker, FeedCrawlWorkerState, FeedPageActor, FeedEvalActor
- **FunkArr.Feed.Tests**: FeedCrawlManagerTests rewritten for new orchestrator shape. New test files for FeedCrawlWorker phases, FeedPageActor, FeedEvalActor. FeedStoreManager tests unchanged
- **FunkArr.Core/ActorKeys.cs**: New marker interface IFeedScheduler
- **FunkArr/AkkaSetupContainer**: Register FeedScheduler singleton, update FeedCrawlManager registration
- **No message changes**: All existing Feed messages (CrawlInput, CrawlResult, QueryFeedCrawlStatus etc.) remain compatible
- **No API changes**: FeedApiEndpoints unchanged
- **No persistence changes**: Cursor events and FeedEntry persistence unchanged
