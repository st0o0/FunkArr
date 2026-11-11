# Proposal: Feed Harvest Streams

## Problem

The Feed crawl pipeline has gone through three architecture rewrites in 9 days
(Nov 02-10) without finding a stable design. The current state:

- **FeedCrawlManager** is a god actor (460 LOC, 3 Become states, 10 internal
  message types) that mixes timer scheduling, persistence, pool management,
  stream dispatch, and run orchestration.
- **FeedCrawlPageActor** merges fetch, filter, grouping, evaluation fan-out, and
  gating into one actor with its own 3-state Become machine.
- **SmallestMailboxPool + Akka.Streams** dispatch provides no real parallelism —
  the MVW Manager's internal Source.Queue (maxConcurrent=3) is the actual
  bottleneck. The pool and stream are overhead without benefit.
- **Sink.Seq** collects all pages in memory, negating stream backpressure.
- Pure logic (filter, group, gate) was inlined into actors during successive
  "simplification" refactors, deleting the standalone tests that existed before.
- 17 internal message types exist for what is conceptually a linear pipeline.

## Solution

Replace the current pool-based dispatch with a clean Akka.Streams pipeline
inside a dedicated **FeedHarvestActor** (long-lived child of the Manager).

### Key decisions

1. **FeedCrawlManager becomes thin**: timer, cursor persistence, status queries,
   options monitoring. No Become states, no pipeline knowledge.

2. **FeedHarvestActor owns the entire pipeline as a single materialized stream**:
   Probe → Resolve → Page Loop → Group → Evaluate → Gate → Publish. One
   `Receive<StartHarvest>` handler, no mutable state, `PipeTo(Sender)` for
   the result.

3. **Sequential page fetching** via `Source.UnfoldAsync`: the MVW Manager
   already throttles HTTP concurrency (maxConcurrent=3). Sequential fetch
   matches the data model (sorted by timestamp desc, stop at cursor). No pool
   needed.

4. **Parallel evaluation** via `SelectAsyncUnordered`: the one place where
   parallelism is genuine — independent ruleset groups evaluated by different
   shard regions.

5. **Logic lives on pipeline types**, not in static helpers or actors:
   `ItemGroup.EvaluateAsync()`, `ScoredGroup.Gate()`,
   `HarvestAccumulator.Add()`. Each type knows how to transform itself into
   the next.

6. **Ask pattern**: Probe uses direct `.SelectAsync(Ask)` + `.Select(transform)`.
   Resolve and Publish use `async SelectAsync` (need to carry values through).
   Page loop uses `async UnfoldAsync` (signature requires it).

### What gets deleted

- `FeedCrawlPageActor.cs` — entire actor
- `FeedCrawlPageActorState.cs` — page-level state
- `FeedCrawlPageMessages.cs` — ProcessPage / ProcessPageResponse
- `FeedCrawlRunState.cs` — ephemeral run state (replaced by stream flow)
- SmallestMailboxPool, IMaterializer on Manager, router management
- 3 Become states + 10 internal message types on Manager

### What stays

- `FeedCrawlManagerState.cs` — persistent cursor state (updated, not replaced)
- `FeedStoreManager` + state — unchanged, separate concern
- `PersistenceMapping.cs` — unchanged
- `Telemetry.cs` — updated for new flow

## Scope

- Feed domain crawl pipeline only
- FeedStoreManager is out of scope (can be revisited separately)
- No changes to Messages or Persistence schemas
- No changes to cross-domain interfaces (Mediathek, RuleSet, Eval regions)
