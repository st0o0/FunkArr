## Context

The Feed domain crawls MediathekViewWeb for new entries, matches them against rulesets, evaluates matches through the Search domain's evaluation workers, gates results by quality/confidence, and publishes accepted entries to FeedStoreManager. The current implementation uses an Akka.Streams pipeline inside a child actor (FeedCrawler) that was extracted from FeedCrawlManager in the feed-stream-redesign change.

The extraction improved separation but left several convention violations: 6 IActorRef constructor params, mutable stream closures, instance methods on state records, and telemetry side-effects in record methods. FeedCrawlManager also mixes three responsibilities: scheduling (timer), persistence (cursor), and orchestration (spawning the crawler). Every other domain in FunkArr uses pure actor patterns — Download, Search, Scoring, and Enrichment have no Akka.Streams code.

## Goals / Non-Goals

**Goals:**
- Rewrite the Feed crawl pipeline using pure actor patterns consistent with the rest of FunkArr
- Eliminate all Akka.Streams usage from the Feed domain (Source, Flow, Sink, IMaterializer)
- Follow all project conventions: Context.GetActor<T>() for deps, Pathfinder state pattern, actor naming (*Manager/*Worker/*Actor)
- Maintain identical crawl behavior: parallel page fetch, parallel evaluation, windowed dedup, gated publishing
- Keep all existing messages, persistence events, and API endpoints unchanged

**Non-Goals:**
- Changing FeedStoreManager (already clean, Pathfinder-compliant)
- Changing the FeedEntry record or persistence format
- Adding new features (new crawl modes, filters, etc.)
- Optimizing crawl performance beyond current levels

## Decisions

### 1. Pure actor pipeline, no Akka.Streams

Replace the 170-line stream pipeline with Become-phase actors and router pools.

**Rationale:** Every other domain (Download, Search, Scoring, Enrichment) uses pure actors. The stream's main value — backpressure — is unnecessary here because the full crawl dataset fits in memory: max 8 pages × 500 items × ~500 bytes ≈ 2 MB. The stream's main cost — mutable closure-captured state (`collected`, `matched`, `pageError`) — was the original motivation for the refactor.

**Alternative considered:** Keep the stream but replace mutable closures with custom GraphStages. This would isolate state in GraphStageLogic fields (thread-safe per Akka docs) but adds a new abstraction layer (GraphStage) that no other domain uses. The actor approach is simpler and consistent.

### 2. Separate FeedScheduler singleton

Extract timer logic from FeedCrawlManager into a dedicated FeedScheduler actor.

**Rationale:** Follows the DownloadScheduler pattern. FeedCrawlManager currently handles three concerns: when to crawl (timer + interval), whether to crawl (cursor + enabled flag), and how to crawl (spawn child, process result). Separating "when" into FeedScheduler leaves FeedCrawlManager as a pure cursor-persistence + orchestration actor.

**Pattern:** `ReceiveActor, IWithTimers`. Reads `FeedOptions.Interval` and `FeedOptions.Enabled`. Sends `StartFeedCrawl` to FeedCrawlManager on each tick. Reacts to option changes via `IOptionsMonitor.OnChange`. Private sealed record `Tick` as timer message.

### 3. SmallestMailboxPool for page fetching

Parallel page fetching via a `SmallestMailboxPool(3)` of FeedPageActor instances, created as children of FeedCrawlWorker.

**Rationale:** Replaces `SelectAsync(3)` from the stream pipeline. The pool size matches the current prefetch parallelism. SmallestMailboxPool is the project-standard pattern (ScoringManager uses it for scoring, EnrichmentManager for TVDB/TMDB calls). FeedPageActor is thin: receives `FetchPage(offset, window)`, Asks MvwManager, filters items by window bounds, detects ReachedEnd, returns `PageResult(offset, items, reachedEnd, error)`.

**Sliding window in FeedCrawlWorker:** The worker fires N initial page requests (where N = pool size), then fires one more for each PageResult received (if not yet reachedEnd). This keeps the pool saturated without over-fetching. The Harvesting phase tracks `nextOffset`, `pendingCount`, and collected items in its state record.

**Alternative considered:** Sequential Ask+PipeTo — simpler but loses parallelism. A single crawl with 8 pages would take ~8× longer. The pool approach preserves the current performance characteristics.

### 4. SmallestMailboxPool for evaluation

Parallel evaluation via a `SmallestMailboxPool(N)` of FeedEvalActor instances (N from `FeedOptions.MaxConcurrency`), created as children of FeedCrawlWorker.

**Rationale:** Same pattern as Decision 3. FeedEvalActor receives an `EvalGroup(ruleSetId, mediaType, ids, items)`, Asks TvEvaluationRegion or MovieEvaluationRegion, runs FeedGate.Evaluate on the result, records telemetry for rejections, and returns `GroupResult(ruleSetId, accepted, error)`.

**Scatter-gather in FeedCrawlWorker:** The Evaluating phase sends all groups to the eval pool at once. State tracks `pendingGroups` (count) and collects `GroupResult` messages. When `pendingGroups` reaches zero, the worker transitions to Publishing.

### 5. FeedCrawlWorker as non-persistent child with 6 Become phases

One FeedCrawlWorker per crawl, spawned by FeedCrawlManager, stopped on completion.

**Phases:**

| Phase | Trigger | Action | Next |
|-------|---------|--------|------|
| Probing | Entry | Ask MvwManager for list timestamp via PipeTo | LoadingRuleSets (or Complete if unchanged) |
| LoadingRuleSets | ProbeResult | Ask RuleSetResolver via PipeTo | Harvesting |
| Harvesting | RuleSetsLoaded | Fire page requests to PageFetchPool, collect PageResults | Evaluating |
| Evaluating | All pages collected | Dedup + match (sync), fire groups to EvalPool, collect GroupResults | Publishing |
| Publishing | All groups collected | Gate (sync), batch entries, Ask FeedStoreManager via PipeTo | Complete |
| Complete | PublishResult | Tell Context.Parent the CrawlResult, Context.Stop(Self) | — |

**Rationale:** Follows TvSearchWorker (4 Become phases) and TvEvaluationWorker (5 phases). Non-persistent because crawl state is ephemeral — if the actor crashes mid-crawl, the next scheduled crawl retries from the same cursor. Router pools are created as the worker's children via `Context.ActorOf(props.WithRouter(...))` — they die automatically when the worker stops.

**State:** `FeedCrawlWorkerState` record follows Pathfinder pattern with `Empty` field and extension methods. Progressive enrichment: `Apply(ProbeResult)` → `Apply(RuleSetsLoaded)` → `Apply(PageResult)` → `Apply(GroupResult)` → `GetCrawlResult()`.

### 6. All actor deps via Context.GetActor<T>() and Context.Parent

Every new actor resolves dependencies through the registry, not constructor params.

| Actor | Deps resolved via |
|-------|-------------------|
| FeedScheduler | `Context.GetActor<IFeedCrawlManager>()` |
| FeedCrawlManager | `Context.GetActor<IFeedStoreManager>()` (for status queries only) |
| FeedCrawlWorker | `Context.GetActor<IMediathekManager>()`, `Context.GetActor<IRuleSetResolver>()`, `Context.GetActor<IFeedStoreManager>()`, `Context.Parent` (for CrawlResult) |
| FeedPageActor | `Context.GetActor<IMediathekManager>()` |
| FeedEvalActor | `Context.GetActor<ITvEvaluationRegion>()`, `Context.GetActor<IMovieEvaluationRegion>()` |

**Rationale:** Project convention. FeedCrawler is the only actor in FunkArr that takes IActorRef constructor params. The DI-only deps (TimeProvider, IOptionsMonitor) still come through the constructor via `resolver.Props<T>()`.

## Risks / Trade-offs

**[Memory from buffering all pages] → Bounded by FeedOptions.MaxPages**
Worst case: 8 pages × 500 items × ~500 bytes = ~2 MB. FeedOptions.MaxPages is already configurable. If the Mediathek grows significantly, the limit can be raised without architectural changes since items are just reference data (strings + numbers).

**[Scatter-gather complexity] → Pathfinder state tracks it cleanly**
The Evaluating phase must track pending vs. completed groups. FeedCrawlWorkerState holds a `pendingGroups` counter that decrements on each `GroupResult`. This is simpler than the stream's GroupBy+MergeSubstreams approach which required careful substream lifecycle management.

**[Loss of stream backpressure for evaluation] → Pool size limits concurrency**
The stream's SelectAsyncUnordered provided automatic backpressure. The eval pool's SmallestMailboxPool(N) achieves the same: N concurrent evaluations max, additional messages queue in the pool router's mailbox. The worker sends all groups at once but the pool processes at most N simultaneously.

**[Page fetch ordering] → Offset-keyed collection**
With parallel page fetching, results arrive out of order. The Harvesting state collects items by offset and detects ReachedEnd per page. Since window-filtering happens in FeedPageActor, items arrive pre-filtered. Dedup happens after all pages are collected, so order doesn't matter for correctness.

**[FeedScheduler adds an actor] → Minimal overhead, cleaner separation**
One more actor in the system. But it's stateless, receives only timer ticks and option changes — identical to DownloadScheduler which has been stable. The benefit is FeedCrawlManager drops ~30 lines of timer management.
