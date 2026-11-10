## Context

FeedCrawlPageActor is a non-persistent pool routee that processes one MVW page: fetch → filter → group by ruleset → evaluate → gate → reply. The Manager orchestrates multiple page actors via an Akka.Streams pipeline.

Current issues:
- `ProcessPage` message carries `RuleSetLookup` (class with `Match()`/`TryMatch()` behavior and internal `Dictionary`) and `CrawlWindow` (domain record) — behavioral objects in messages violate the project convention of primitive params.
- `PageProcessed` is a flat record with nullable `Error` and nullable `RejectedByReason` — no abstract response hierarchy.
- Manager uses a hand-rolled actor array with manual round-robin modulo dispatch instead of an Akka router.
- Stream `SelectAsync` lambda captures `run.Window` and `run.Lookup` — actor context access from stream threads.

## Goals / Non-Goals

**Goals:**
- Messages follow project conventions: flat data in, abstract response + Completed/Failed out.
- Pool management uses Akka's SmallestMailboxPool router with dynamic resize.
- Stream pipeline has zero actor-context access (all data prepared before stream starts).
- PageActor is safe against message loss via IWithUnboundedStash.

**Non-Goals:**
- Splitting FeedCrawlPageActor into multiple actors (fetch/evaluate/gate stages). The actor's single-responsibility is "process one page end-to-end" — that stays.
- Changing FeedEntry shape or FeedGate logic.
- Changing persistence events or snapshot format.

## Decisions

### Decision 1: SmallestMailboxPool over RoundRobinPool

SmallestMailboxPool routes to the routee with the fewest queued messages. PageActors have variable processing time (1 fetch + N evaluations depending on ruleset matches). SmallestMailbox naturally favors idle actors over busy ones.

RoundRobinPool would blindly rotate, potentially queueing behind a slow actor while another is idle.

Alternative considered: No pool, fresh actor per page. Rejected because it creates up to MaxPages actors (potentially hundreds) and provides no reuse.

### Decision 2: Permanent pool with AdjustPoolSize

The router is created once in RecoveryCompleted and lives for the Manager's lifetime. When `MaxConcurrency` changes via OptionsMonitor, the Manager sends `AdjustPoolSize(newSize - currentSize)` to resize dynamically.

Alternative considered: Recreate pool per crawl run. Rejected because it adds Stop/Create overhead and risks actor name collision if a run is somehow still draining.

Alternative considered: Akka Resizer. Rejected because it reacts to mailbox pressure, not configuration — we want explicit control tied to FeedOptions.

### Decision 3: IWithUnboundedStash for message safety

SmallestMailboxPool inspects mailbox size, not actor Become state. A PageActor in Fetching with an empty mailbox looks "free" to the router. If a second ProcessPage arrives, it must be stashed (not dropped as unhandled).

Stash in Fetching and Evaluating states, UnstashAll on return to Ready. This is the standard Akka pattern for actors that temporarily cannot process certain message types.

### Decision 4: ProcessPage carries RuleSetTarget[] instead of RuleSetLookup

`RuleSetTarget` is already an existing internal record (`string RuleSetId, MediaType MediaType, ExternalIds Ids`). The message carries the flat array; the PageActor builds a `RuleSetLookup` internally from it. This keeps behavior out of messages.

`CrawlWindow` is replaced by two primitive `long` fields (`LowerBound`, `UpperBound`). The window's `Truncated` flag is not needed by the PageActor — it's only used by the Manager for result reporting.

### Decision 5: Stream-native Ask stage

Replace `SelectAsync(n, offset => router.Ask<T>(...))` with `Source.From(messages).Ask<T>(router, timeout, parallelism)`. The `ProcessPage[]` array is built in the actor thread before the stream starts, eliminating all closure captures over actor state.

### Decision 6: RuleSetLookup gets ToTargets() for Manager use

The Manager already holds a `RuleSetLookup` (built from `RegisteredRuleSetsResult`). Adding `ToTargets()` extracts the `RuleSetTarget[]` needed for message construction. The PageActor has a new static factory `RuleSetLookup.From(RuleSetTarget[])` that rebuilds the lookup from targets — this already exists as `From(IEnumerable<RegisteredRuleSetEntry>)` and gets a second overload.

## Risks / Trade-offs

- **[Risk] Stash memory under sustained load**: If the router consistently sends more messages than actors can process, stash grows. → Mitigation: SelectAsync parallelism = pool size, so at most N in-flight Asks for N actors. Stash depth is bounded to 1 per actor in practice.
- **[Risk] AdjustPoolSize during active crawl**: Shrinking the pool while actors are processing could stop a busy routee. → Mitigation: OptionsChanged during Crawling/Probing defers the resize to ReturnToIdle.
- **[Trade-off] RuleSetLookup rebuilt per page**: Each PageActor builds a RuleSetLookup from the target array on every ProcessPage. This is a small Dictionary construction (~20-50 entries). Negligible cost vs. the HTTP calls that follow.
