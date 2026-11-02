# Proposal: Crawl Manager Rewrite

## Problem

The `FeedCrawlManager` already uses a long-lived Akka.Streams pipeline (`Source.Queue` → `ConcatMany` → `Sink.ActorRef`), but the probe and ruleset-loading steps run outside the stream as separate `Ask`/`PipeTo` chains. This creates two separate orchestration layers: pre-stream (actor message handlers with `StartRun` → `HandleProbe` → `Offer`) and in-stream (the `Crawl()` pipeline). The `FeedCrawlPlan` and `FeedCrawlRun` records bridge these layers, adding concepts (plan creation, run tracking with `RunId`, `WhenCurrent` guards, `Apply` chains) that wouldn't exist if everything lived in the stream.

## Goal

Rewrite the `FeedCrawlManager` so the entire crawl — from probe through publish — runs inside a single stream pipeline. The actor becomes a thin shell: timer, queue offer, persistence, telemetry counters. No `FeedCrawlPlan`, no `FeedCrawlRun`, no `RunId`.

Model: the `MediathekViewWebManager` pattern where the actor offers a signal into the queue and the stream does all the work, sending results back via `Sink.ActorRef(Self)`.

## Scope

**In scope:**
- Move probe and ruleset loading into the stream
- Replace `FeedCrawlPlan` with a `CrawlContext` record built inside the stream
- Replace `FeedCrawlRun` progress tracking with simple counters on the actor
- Remove `RunId` and `WhenCurrent` guards (queue backpressure handles concurrency)
- Introduce typed outcome messages (`FeedCrawlCompleted`, `FeedCrawlUnchanged`, `FeedCrawlFailed`) instead of a single `FeedCrawlEvent` hierarchy
- Keep progress events (WireTap → Self) for telemetry

**Out of scope:**
- Changing the stream's inner pipeline logic (page fetching, dedup, matching, evaluation, gating, publishing)
- Changing persistence schema or cursor semantics
- Changing `FeedCrawlPlanner` pure functions (window calculation, page evaluation, dedup)
- Changing `FeedStoreManager` or evaluation worker interaction

## Approach

### Stream shape

```
Source.Queue<Signal>(1, DropNew)
  .SelectAsync(1, Probe)              → ProbeResult
  .ConcatMany(Dispatch)               → branch: unchanged or full crawl
    ├─ unchanged → Source.Single(FeedCrawlUnchanged)
    └─ HasUpdate:
         Source.FromTask(LoadContext)   → CrawlContext
           .ConcatMany(RunCrawl)       → full pipeline (pages → eval → publish)
           .Recover(→ FeedCrawlFailed)
  .To(Sink.ActorRef(Self))
```

### Records at each stage boundary

| Boundary | Record | New/Existing |
|----------|--------|-------------|
| Queue → Probe | `Signal` (unit) | New |
| Probe output | `ProbeResult(long ListTimestamp)` | New |
| After rulesets | `CrawlContext(ListTimestamp, Window, Lookup, PageSize, MaxPages, MinConfidence, LeadLimit)` | New (replaces FeedCrawlPlan) |
| Page fetch | `PageEvaluation(Items, ReachedEnd)` | Existing |
| Item level | `MediathekItem` | Existing |
| After match | `FeedMatch(Item, Target)` | Existing |
| After eval | `FeedEvaluation(Group, Results, Error)` | Existing |
| After gate | `FeedGated(RuleSetId, Accepted, Error)` | Existing |
| After publish | `BatchResult(Groups, FailedGroups, Published)` | New |
| Aggregate output | `CrawlTotals(Groups, FailedGroups, Published)` | New |
| Stream → Actor | `FeedCrawlCompleted` / `FeedCrawlUnchanged` / `FeedCrawlFailed` | Refactored |

### Actor simplification

- **No `_run` field** — simple `_collected`/`_matched` counters, reset on outcome
- **No `RunId`** — `Source.Queue(1, DropNew)` ensures one crawl at a time
- **No `WhenCurrent` guards** — stream ordering guarantees progress events arrive before outcome
- **No `StartRun`/`HandleProbe`/`Offer` handler chain** — actor just offers `Signal` on tick
- **Telemetry from progress events** — WireTaps send `FeedItemCollected`/`FeedItemMatched`/`FeedPageCrawled` to Self
- **`HandleCompleted` combines** stream outcome (published, failed groups, gap) with actor counters (collected, matched) → persistence event

### What stays

- `FeedCrawlPlanner` (pure functions: `Window`, `EvaluatePage`, `DistinctByStableId`, `HasUpdate`)
- `FeedCrawlManagerState` (cursor, persistence, decision logic)
- Persistence events (`FeedCursorAdvanced`, `FeedRunFailed`)
- All inner pipeline records (`FeedMatch`, `FeedEvaluation`, `FeedGated`, `FeedGate`)
- Timer scheduling pattern (schedule next after outcome)
- `FeedOptions` configuration

### What goes

- `FeedCrawlPlan` → replaced by `CrawlContext`
- `FeedCrawlRun` + `FeedCrawlRunExtensions` → replaced by actor counters
- `FeedCrawlEvent` hierarchy (7 internal record types) → 3 outcome types + 3 progress types
- `CrawlOffered` / `Probed` / `RuleSetsLoaded` / `RuleSetsFailed` internal messages → absorbed into stream
- `WhenCurrent(runId)` pattern → unnecessary

## Risks

- **Dispatch nesting** (`ConcatMany` → `FromTask` → `ConcatMany`): two levels of ConcatMany. Akka.Streams handles this fine — inner sources complete before the outer ConcatMany pulls the next element. Monitor for clarity in error messages if the inner FromTask fails.
- **RuleSet load failure inside stream**: currently handled as a PipeTo failure message. In the stream, it becomes an exception in `FromTask` caught by `Recover`. Same behavior, different mechanism.
- **Counter drift**: if a WireTap message gets lost (actor restart mid-crawl), counters will be wrong. Acceptable — telemetry counters are best-effort, and the actor restart re-materializes the stream anyway.
