## Context

FeedCrawlManager orchestrates feed crawling through 5 Become-states (Idle → Probing → LoadingRuleSets → Crawling → Publishing) with manual page dispatch to a SmallestMailboxPool of FeedCrawlPageActors. The PageActor uses IWithUnboundedStash to buffer messages during Fetching/Evaluating states, which hides mailbox depth from the pool router and defeats its routing heuristic. The Manager manually tracks PendingPages/PagesFired/NextOffset to control concurrency — reimplementing backpressure that Akka.Streams provides natively.

The PageActor also violates project conventions: mutable state fields live directly on the actor instead of a State object, it uses `_manager.Tell` instead of `Sender.Tell`, and static readonly fields use `_camelCase` instead of PascalCase.

## Goals / Non-Goals

**Goals:**

- Replace manual page dispatch (Tell-loop + PendingPages tracking) with Akka.Streams `SelectAsync` + `Ask` + `Sink.Seq` + `PipeTo`
- Remove IWithUnboundedStash from PageActor — SelectAsync parallelism matching pool size ensures each routee receives at most one message
- Collapse Manager Become-states from 5 to 3 by merging Probing+LoadingRuleSets and Crawling+Publishing
- Inline small handler methods as lambdas to reduce method count
- Clean up PageActor to follow Pathfinder conventions (State object, Sender-based reply, PascalCase statics)
- Slim FeedCrawlRunState by removing dispatch-only fields and methods

**Non-Goals:**

- Replacing the PageActor with a pure async function (actor stays for Become-based internal pipeline)
- Replacing SmallestMailboxPool with a different router (pool stays, just works correctly now)
- Changing ProcessPage/PageProcessed message shapes
- Changing persistence events or FeedCrawlManagerState
- Adding new features or changing crawl behavior

## Decisions

### Decision: SelectAsync + Ask instead of ActorFlow.Ask

`ActorFlow.Ask` does not exist in Akka.NET Streams 1.5.74 (JVM-only API). Use `SelectAsync(parallelism, offset => pool.Ask<PageProcessed>(msg, timeout))` which provides identical behavior: max N concurrent Asks with built-in backpressure.

**Alternative considered**: `Sink.ActorRef` streaming individual PageProcessed to Manager. Rejected because it requires the Manager to still count pending pages and handle a completion signal separately. `Sink.Seq` + `PipeTo` delivers all results in one message — simpler Manager code.

### Decision: Sink.Seq + PipeTo delivers batch result

The materialized `Task<IImmutableList<PageProcessed>>` is piped to Self as a single `CrawlPagesCompleted` message. The Manager aggregates all results at once via `ApplyAll()` instead of per-page. This eliminates the need for PendingPages tracking and the "check if all done" logic.

**Trade-off**: Manager no longer sees individual page results as they arrive. This is acceptable because the Manager takes no action between pages except dispatch (which the stream now handles) and aggregation (which happens identically in batch).

### Decision: TakeWhile for dynamic stop

`TakeWhile(r => !r.ReachedEnd, inclusive: true)` stops the stream after the first page that reports reachedEnd, while still including that page's results. In-flight Asks from SelectAsync complete but their results are discarded by the closed stream — matching current behavior where the Manager over-fires initial pages.

### Decision: Merge Probing + LoadingRuleSets into one Become-state

The transition Probing → LoadingRuleSets is a linear Ask chain (probe MVW → load rulesets). Both states handle the same background commands (Tick, OptionsChanged, Status, Snapshots). Merging them into a single Probing state with inline handlers for ProbeResult → Ask rulesets → RuleSetsLoaded → StartCrawling reduces method count without losing clarity.

### Decision: Merge Crawling + Publishing into one Become-state

After CrawlPagesCompleted, the Manager either publishes (Ask FeedStoreManager) or completes. Publishing is a single Ask with two outcomes. Keeping it as a separate Become-state added 3 methods (StartPublishing, HandlePublishCompleted, HandlePublishFailed) for what is essentially one more step in the same flow. Merging into Crawling with inline handlers keeps the full crawl lifecycle in one place.

### Decision: PageActor captures Sender as _replyTo

PipeTo overwrites Sender in subsequent handlers (Fetching receives FetchCompleted from Self, not from the original Ask caller). The actor must capture the original Sender (the stream's temporary Ask actor) in HandleProcessPage and reply to `_replyTo` in ReplyAndBecomeReady. This is required for Ask-based dispatch.

### Decision: PageActor State object despite non-persistent actor

The Pathfinder convention requires all mutable domain state in a State object unconditionally. Even though FeedCrawlPageActor is non-persistent, the State object provides: clean reset via `Empty` instead of manual field clearing, `Apply` methods for structured mutations, and consistency with the rest of the codebase.

### Decision: Keep SmallestMailboxPool

The pool router stays. With SelectAsync parallelism = pool size, each routee gets at most one message, so SmallestMailboxPool works correctly (all routees have equal mailbox depth of 0 or 1). No Stash means a routing anomaly results in dead letters → Ask timeout → visible stream failure, which is the right behavior.

## Risks / Trade-offs

- **In-flight Asks after TakeWhile closes**: When one page reports reachedEnd, other in-flight Asks complete but results are discarded. Wasted work but not harmful — matches current behavior. → No mitigation needed.
- **SmallestMailboxPool routing anomaly**: If the pool routes a second ProcessPage to a busy routee (theoretically possible with equal mailbox counts), the message goes to dead letters, the Ask times out, and the stream fails with CrawlPagesFailed. → Visible and debugbar. The Manager handles this as a crawl failure with retry on next tick.
- **Thread safety of stream closures**: SelectAsync lambdas run on Materializer threads, not the Actor thread. All captured values are either immutable (IActorRef, records, static readonly TimeSpan) or read-only after construction (RuleSetLookup). RunState mutation happens only in PipeTo callback on Actor thread. → No shared mutable state.
- **Sink.Seq memory**: All PageProcessed results are held in memory until the stream completes. With MaxPages typically ≤10 and each PageProcessed being small (FeedEntry array + a few ints), this is negligible. → No mitigation needed.
