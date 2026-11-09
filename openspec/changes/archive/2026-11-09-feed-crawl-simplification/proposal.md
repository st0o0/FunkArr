## Why

The Feed crawl pipeline has 6 actor types across 3 hierarchy levels (Manager → Worker → Pools). FeedCrawlWorker is a god-actor orchestrating 5 phases with child pools. FeedPageActor and FeedEvalActor are thin proxies that add indirection without value. FeedEvalActor has a latent stale-sender bug (_replyTo overwrite risk in the pool). The batch-collect-then-evaluate pattern holds all items in memory before processing. FeedScheduler exists only for a timer + config watch that the Manager can do itself.

## What Changes

- **Eliminate FeedCrawlWorker** — orchestration logic absorbed by FeedCrawlManager's Become state machine
- **Eliminate FeedPageActor and FeedEvalActor pools** — replaced by a single FeedCrawlPageActor pool where each routee independently fetches a page, matches against rulesets, scores via Search evaluation regions, and applies FeedGate
- **Eliminate FeedScheduler** — scheduling absorbed by FeedCrawlManager (IWithTimers + IOptionsMonitor)
- **Introduce FeedCrawlPageActor** — pool routee that processes one page end-to-end (fetch → match → score → gate → reply)
- **Flatten to 2 levels** — FeedCrawlManager with a single page-pool child, no grandchildren
- **Per-page pipeline** — items are evaluated per page instead of batch-collected across all pages first
- **Transient run state separation** — FeedCrawlRunState holds crawl session data (nullable, not persisted), separate from persistent FeedCrawlManagerState

## Capabilities

### New Capabilities

- `feed-crawl-page-actor`: Pool routee that fetches one MVW page, groups items by ruleset, evaluates via Search domain, applies FeedGate, and returns accepted entries plus stats to the Manager.

### Modified Capabilities

- `feed-actor-topology`: Actor topology changes from 6 types / 3 levels to 3 types / 2 levels. FeedScheduler, FeedCrawlWorker, FeedPageActor, FeedEvalActor eliminated.
- `feed-crawl-lifecycle`: FeedCrawlManager becomes the orchestrator with Become states (Idle → Probing → LoadingRuleSets → Crawling → Publishing). Absorbs scheduling from FeedScheduler.
- `feed-page-fetch`: Page fetching moves from FeedPageActor (proxy) into FeedCrawlPageActor (inline). Window filtering and dedup happen inside the page actor.
- `feed-eval-gate`: Evaluation and gating move from FeedEvalActor (proxy) into FeedCrawlPageActor (inline per page). Stale-sender bug eliminated by Become(Ready) pattern.
- `feed-partial-harvest`: Partial harvest semantics preserved but applied per-page. A page error results in an error PageProcessed; other pages continue independently.

## Impact

- **Feed domain**: Major refactor — 8 files deleted, 3 new files, 2 files rewritten
- **Akka setup**: AkkaSetupContainer loses FeedScheduler singleton registration, gains page-pool setup
- **Tests**: FeedCrawlManagerTests rewritten, FeedCrawlWorkerStateTests deleted, new FeedCrawlPageActorTests and FeedCrawlRunStateTests
- **Messages**: No public message changes — ProcessPage/PageProcessed are domain-internal
- **Persistence**: No event or snapshot changes — same FeedCursorAdvanced/FeedRunFailed events
- **API**: No endpoint changes — FeedStoreManager unchanged
- **Other domains**: No impact — Search evaluation regions, RuleSetResolver, MediathekManager interfaces unchanged
