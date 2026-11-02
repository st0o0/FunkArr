## Context

FeedCrawlManager is the only domain actor that combines orchestration and execution in a single class. Its `StartCrawl()` method builds a ~250-line inline Akka.Streams graph that handles probing, paging, filtering, ruleset matching, evaluation dispatch, gating, publishing, and result aggregation — all with seven mutable closure-captured variables (`lookup`, `window`, `listTimestamp`, `shortCircuit`, `collected`, `matched`, `pageError`).

Every other domain separates these concerns:
- **Download**: DownloadManager (queue orchestration) → DownloadWorker (per-item execution) → IRemuxer/ISubtitleDownloader (services)
- **Search**: SearchManager (routing) → TvSearchWorker/MovieSearchWorker (per-request lifecycle with Become phases) → EvaluationWorkers (per-evaluation)
- **RuleSet**: RuleSetManager (coordination) → RuleSetWorker (per-ruleset) + RuleSetValidator/RuleSetMerger (pure services)

Feed also has a domain model boundary violation: `FeedEntry` wraps `SearchResultItem` directly, creating a compile-time dependency from Feed's core type to Search's result type.

The existing `crawl-stream-pure` spec captures good pipeline principles (errors-as-data, counting-in-closures, CrawlInput-carries-context, single CrawlResult output) that remain valid. The change is structural: where those stages execute, not what they produce.

## Goals / Non-Goals

**Goals:**
- Bring Feed's actor topology to the same manager/worker separation as Download and Search
- Eliminate mutable closure state by completing all setup before stream materialization
- Give Feed its own domain model, removing the `SearchResultItem` wrapper from `FeedEntry`
- Keep the stream for what it's good at (batched page→eval→publish with backpressure) while moving sequential setup into actor phases
- Maintain all existing behavior: cursor advancement, failure tracking, telemetry, unchanged detection, gap handling

**Non-Goals:**
- Changing FeedStoreManager (already clean, follows project patterns)
- Changing persistence event schemas (PersistedFeedEntry already has flat fields)
- Adding new features or changing crawl behavior
- Making FeedCrawler persistent (it's a single-run child, not a long-lived entity)
- Sharding feed crawl work (single crawl run is correct for this domain)

## Decisions

### Decision 1: FeedCrawler as non-persistent child actor

**Choice**: Create `FeedCrawler` as a non-persistent child actor spawned by FeedCrawlManager for each crawl run, using Become-based phases.

**Why not a service class**: The probe phase requires Ask-based communication with MediathekManager and RuleSetResolver. An actor can receive responses naturally via Become; a service would need `async Task` with `Ask` and lose Akka's threading guarantees.

**Why not a sharded entity**: A crawl run is ephemeral (seconds to minutes), not a long-lived entity. There's exactly one active crawl at a time. Child actor lifecycle is managed by the parent — no passivation, no persistence ID, no shard overhead.

**Why not keep it inline**: The 250-line method with seven mutable closures is the core problem. Extracting to a child actor gives clean lifecycle (stop on completion/failure), isolated state (FeedCrawlerState), and testability (spawn in TestKit, assert on messages back to parent).

**Pattern reference**: TvSearchWorker uses exactly this approach — Become-based phases (ResolvingRuleSet → Querying → Evaluating), mutable state in a dedicated state record, reply to parent on completion.

**Lifecycle**:
```
FeedCrawlManager                     FeedCrawler (child)
     │                                    │
     │── Context.ActorOf(crawler) ───────▶│ Initial
     │                                    │── Ask MediathekManager (probe)
     │                                    │── Ask RuleSetResolver
     │                                    │── Compute CrawlWindow
     │                                    │   Become(Crawling)
     │                                    │── Materialize stream
     │                                    │── PipeTo(Self)
     │◀── CrawlResult (Tell to parent) ──│
     │── Context.Stop(crawler) ──────────▶│ Stopped
     │── Persist cursor
     │── ScheduleNext
```

### Decision 2: Become phases for sequential setup, stream for batch processing

**Choice**: The crawler uses two phases:
1. **Probing** — sequential Ask calls (MediathekManager for list timestamp, RuleSetResolver for rulesets), compute CrawlWindow. All via Become-based message handling.
2. **Crawling** — materialize stream with all context as immutable input. Stream handles: pages → filter → dedup → match → evaluate → gate → publish → aggregate.

**Why hybrid**: The probe/setup phase is inherently sequential (need timestamp before window, need rulesets before matching). The page processing phase is inherently parallel (pages fetched concurrently, evaluations dispatched concurrently, results batched). Using Become for sequential work and streams for parallel work matches each tool to its strength.

**What changes in crawl-stream-pure**: The "probe as SelectAsync" and "ConcatMany branch point" requirements no longer apply — probe happens before the stream exists. The stream starts at page offsets, not at CrawlInput. Errors-as-data, counting-in-closures, and single-CrawlResult-output all carry forward unchanged.

### Decision 3: Immutable context passed to stream

**Choice**: After the probe phase completes, the crawler builds an immutable `CrawlContext` record containing everything the stream needs:

```
CrawlContext(
    CrawlInput Input,        // cursor, page size, max pages, concurrency, etc.
    long ListTimestamp,       // from probe
    CrawlWindow Window,      // computed from probe + cursor
    RuleSetLookup Lookup,    // from RuleSetResolver
    IActorRef Mediathek,     // actor refs for Ask
    IActorRef TvEvaluation,
    IActorRef MovieEvaluation,
    IActorRef Store)
```

The stream captures this single record. No nullable closures, no mutation during stream execution. The seven mutable variables from the current implementation disappear.

**Counting stays in closures**: Per crawl-stream-pure, `collected` and `matched` counters remain as closure-captured `int` variables inside the stream builder method. These are write-only within the stream and read once at aggregation — safe because stream stages run sequentially per element.

### Decision 4: FeedEntry with owned fields

**Choice**: Replace `FeedEntry(StableId, FirstSeenAt, MediaType, SearchResultItem Item)` with flat fields:

```
FeedEntry(
    StableId, FirstSeenAt, MediaType,
    Title, Channel, Topic, Url,
    Duration, Size, Quality,
    AiredAt, Score, SubtitleUrl,
    ExternalIds? Ids, int? Season, int? Episode)
```

**Why**: Removes compile-time dependency from `FunkArr.Messages.Feed` on `FunkArr.Messages.Search`. The persistence layer (PersistedFeedEntry) already has these flat fields — the current PersistenceMapping manually unpacks `Item.Title`, `Item.Channel`, etc. With owned fields, the mapping simplifies to direct field correspondence.

**Migration impact**:
- `FeedGate.Evaluate()` constructs FeedEntry with flat fields instead of wrapping SearchResultItem
- `NewznabSearchService.ToFeedRss()` passes FeedEntry fields to `ToItem()` instead of `entry.Item`
- `FeedApiEndpoints.cs` reads `entry.Title` instead of `entry.Item.Title`
- `PersistenceMapping` simplifies (flat → flat instead of nested → flat)
- No persistence event changes (PersistedFeedEntry is already flat)

### Decision 5: FeedCrawlManager becomes thin orchestration

**Choice**: FeedCrawlManager keeps only:
- Persistence (cursor state, snapshot, recovery)
- Timer scheduling (Tick → trigger crawl)
- Spawning/stopping FeedCrawler child
- Handling CrawlResult → persist → reschedule
- Status queries (QueryFeedCrawlStatus)

**What moves out**: Everything in `StartCrawl()` after the guard clauses. The manager builds `CrawlInput` from options + state, spawns a FeedCrawler, and returns. The crawler reports back via a Tell.

**Guard against concurrent runs**: The `_running` flag stays in FeedCrawlManager. While a crawler child is alive, new Tick/StartFeedCrawl messages are ignored (same as today). When CrawlResult arrives, `_running` resets and the child is stopped.

### Decision 6: Internal types stay in Feed domain, reorganized by owner

**Choice**:
- `CrawlResult.cs` keeps: `CrawlInput`, `CrawlWindow`, `CrawlResult`, `CrawlStatus` (used by both manager and crawler)
- New `FeedCrawlerTypes.cs`: `PageEvaluation`, `FeedMatch`, `EvalResult`, `GatedResult`, `CrawlBatchResult`, `CrawlAccumulator` (internal to crawler's stream)
- `RuleSetLookup.cs`, `FeedGate.cs`: unchanged, used by crawler

No type leaves the Feed project boundary. The reorganization is about ownership clarity, not visibility.

## Risks / Trade-offs

**[Child actor lifecycle complexity]** → Mitigation: FeedCrawlManager already tracks `_running` state. Stopping the child on CrawlResult (or on timeout if we add one) is straightforward. The child has no persistence, no subscriptions, no watched actors — clean stop is safe.

**[Two-phase probe can fail between steps]** → Mitigation: If the MediathekManager probe succeeds but RuleSetResolver times out, the crawler reports a CrawlResult.ForFailed back to the manager. Same error handling path as today, just triggered from actor messages instead of stream stages. Each phase has its own timeout.

**[FeedEntry breaking change]** → Mitigation: Version 0.x, breaking changes are fine (per CLAUDE.md). Only two consumers outside Feed: `FeedApiEndpoints.cs` and `NewznabSearchService.ToFeedRss()`. Both adapt by reading flat fields instead of nested `.Item.*` — a mechanical change, not a logic change.

**[Stream graph still uses closures for counting]** → Accepted trade-off: Per crawl-stream-pure spec, counting in closures is the intended pattern. The difference is that the closures now capture only `int` counters (safe, write-only during stream, read at aggregation), not nullable reference types that get assigned mid-execution.

**[crawl-stream-pure spec divergence]** → Mitigation: The "Single ConcatMany at the page fan-out" and "Probe and rulesets as SelectAsync" requirements need updating since probe moves out of the stream. A delta spec captures this change explicitly. The other three requirements (errors-as-data, counting-in-closures, CrawlInput-carries-context) remain valid.
