## Context

The Feed domain crawls MediathekViewWeb periodically, evaluates new entries against registered rulesets via the Search domain, and stores accepted entries for RSS consumption. The current implementation uses 6 actor types across 3 hierarchy levels: FeedScheduler → FeedCrawlManager → FeedCrawlWorker → (FeedPageActor pool + FeedEvalActor pool). The Worker is a god-actor that orchestrates 5 Become phases, spawns two child pools, and batch-collects all items in memory before evaluation. FeedPageActor and FeedEvalActor are thin proxies adding indirection without value.

## Goals / Non-Goals

**Goals:**
- Flatten to 3 actor types / 2 hierarchy levels: FeedCrawlManager → FeedCrawlPageActor pool, plus FeedStoreManager
- Process items per-page instead of batch-collecting across all pages
- Eliminate the stale-sender bug in FeedEvalActor's pool usage
- Keep all existing persistence events, public messages, and API contracts unchanged
- Maintain existing crawl outcome semantics (Completed, CompletedWithGap, Failed, Unchanged)

**Non-Goals:**
- Changing FeedStoreManager or its persistence model
- Changing the public Messages/Feed/ contract
- Changing CrawlResult, FeedGate, or RuleSetLookup behavior
- Akka.Streams (was previously considered and rejected; pool+Become is the project pattern)
- Changing how Search evaluation regions (TvEvaluation/MovieEvaluation) work

## Decisions

### D1: Absorb FeedScheduler into FeedCrawlManager

FeedScheduler exists only to hold a timer and watch IOptionsMonitor. FeedCrawlManager already has access to IOptionsMonitor<FeedOptions> and can implement IWithTimers. The scheduling state (interval, enabled) merges into FeedCrawlManagerState as transient fields (not persisted — derived from config on startup).

**Alternative considered**: Keep FeedScheduler as a separate actor. Rejected because it adds an actor that does nothing but forward a single Tell on a timer — the Manager can self-schedule with one `Timers.StartSingleTimer` call.

### D2: FeedCrawlManager as Become state machine with 5 states

The Manager uses Become() to transition through: Idle → Probing → LoadingRuleSets → Crawling → Publishing → Idle. Every state registers the `QueryFeedCrawlStatus` handler so the Manager remains responsive. The `StartFeedCrawl` command is only accepted in Idle (other states ignore it — the crawl is already running).

**State separation:**
- `FeedCrawlManagerState` (persistent): cursor, consecutive failures, last run time, last outcome. Applied via `Persist()`, snapshotted. Same persistence events as today.
- `FeedCrawlRunState` (transient): pending page count, next offset, reached-end flag, collected entries, stats counters, window, lookup. Created when entering Probing, nulled when returning to Idle. NOT persisted — crash recovery replays only the persistent state, and the next timer tick starts a fresh crawl.

**Alternative considered**: Single flat state class with nullable fields for run data. Rejected because the persistent/transient boundary is a clear contract — mixing them risks accidentally persisting transient crawl data or losing persistent cursor data.

### D3: Per-page processing via FeedCrawlPageActor pool

Instead of collecting all pages then evaluating all items, each page is processed end-to-end by a single pool routee:

1. Receive `ProcessPage(offset, window, pageSize, lookup, minConfidence)`
2. Ask MediathekManager for the page → window filter → dedup by stable ID
3. Group filtered items by ruleset via RuleSetLookup
4. For each group: Ask TvEvaluationRegion or MovieEvaluationRegion
5. Apply FeedGate.Evaluate per group
6. Reply `PageProcessed(accepted, newItems, unmatched, reachedEnd, error)` to sender

The actor uses Become: Ready → Fetching → Evaluating → (reply, Become Ready). The Become(Ready) transition after completion ensures a routee never processes two pages concurrently — this eliminates the stale-sender bug from the old FeedEvalActor.

**Pool configuration**: `SmallestMailboxPool` sized by `FeedOptions.MaxConcurrency` (default 4). Created as a permanent child of FeedCrawlManager in the constructor/recovery phase. The pool survives across crawl cycles — no repeated pool creation/destruction.

**Alternative considered**: No pool, just bounded outstanding Ask+PipeTo from the Manager. Rejected because pools give natural concurrency control, are testable with TestProbes, and keep the Manager's message handling simple (one PageProcessed handler vs. tracking N outstanding asks with state).

### D4: Crawling phase — sliding window page dispatch

The Manager in Crawling state uses the same sliding-window dispatch as the current Worker:
- Fire `prefetch` initial pages into the pool
- On each PageProcessed: if more pages needed and not reached end, fire the next page
- Track pending count in FeedCrawlRunState
- When pending reaches 0: transition to Publishing (if entries collected) or back to Idle (if nothing to publish)

The FeedCrawlRunState accumulates accepted FeedEntry[] and stats (newItems, unmatched, published) from each PageProcessed response. This replaces the current FeedCrawlWorkerState.

### D5: ProcessPage carries RuleSetLookup and CrawlWindow

Each ProcessPage message carries the lookup table and window so the page actor is fully self-contained — no Context.GetActor calls for rulesets, no shared mutable state. The lookup and window are created once per crawl (in LoadingRuleSets) and passed through.

The page actor still uses Context.GetActor for MediathekManager, TvEvaluationRegion, and MovieEvaluationRegion — these are actor registry lookups, not per-crawl data.

### D6: Telemetry recording stays in the Manager

CrawlResult-level telemetry (crawl duration, outcome, gaps) stays in the Manager's result handling. Per-page telemetry (items seen, published, rejected reasons) moves to either the PageActor (for rejection reasons) or the Manager's PageProcessed handler (for aggregate counters). The Telemetry static class is unchanged.

### D7: Error handling — per-page isolation

A page fetch failure or evaluation failure results in a PageProcessed with an error field. The Manager treats it like the current partial-harvest logic: collect what succeeded, continue processing other pages. A crawl with any page errors but some collected items results in CompletedWithGap. A crawl where all pages fail results in Failed.

The FeedCrawlRunState tracks whether any page had errors (for gap detection) separately from whether items were collected (for cursor advancement).

## Risks / Trade-offs

**[ProcessPage message size]** → ProcessPage carries a RuleSetLookup per message, which contains the full topic→target dictionary. For typical installations (10-50 rulesets) this is negligible. If ruleset count grows to thousands, consider sharing the lookup via a shared reference rather than per-message copy. Mitigation: RuleSetLookup is a reference type, so it's the same object instance passed to all pool messages within one crawl — no deep copy.

**[Pool routee lifetime]** → Permanent pool routees accumulate across crawls. If a routee gets stuck (e.g., MediathekManager never responds), it blocks one pool slot. Mitigation: Ask timeouts on all external calls (30s for MVW, 60s for evaluation). Timeout → error PageProcessed → Manager continues.

**[Manager complexity]** → The Manager gains 5 Become states, up from 3 command handlers. Each state is 2-3 handlers with delegation to state classes. The actor code is thin plumbing; logic lives in FeedCrawlManagerState, FeedCrawlRunState, FeedGate, and RuleSetLookup. Trade-off accepted: one slightly larger actor is simpler than 4 extra actor types.

**[Persistence ID unchanged]** → FeedCrawlManager keeps persistence ID "feed-crawl" and the same event types. Existing journals replay correctly. No migration needed.

**[FeedScheduler removal]** → The IFeedScheduler actor key in AkkaSetupContainer is removed. Any code referencing IFeedScheduler will fail at compile time (good — forces cleanup). The FeedSchedulerState fields (interval, enabled) merge into the Manager as config-derived transient state.
