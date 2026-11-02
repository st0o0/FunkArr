# Proposal: Flat Crawl Stream

## Problem

The FeedCrawlManager still uses a long-lived `Source.Queue` stream with `ConcatMany` — a stream-in-stream pattern. No other stream in FunkArr does this. The MediathekViewWebManager, DownloadManager, RuleSetManager, and StatsCollector all use flat streams: either `Source.Queue → stages → Sink.ActorRef` (long-lived) or `Source.From → stages → RunWith(Sink) → PipeTo` (short-lived). The CrawlManager also still uses `CrawlContext` as a god-object with methods (Read, Match, WithinLead, Group, etc.) and `FeedCrawlPlanner` as a static helper class — abstractions that no other FunkArr stream needs.

## Goal

One flat short-lived stream per crawl. `Source.Single(input)` → prepare → `SelectMany(offsets)` → flat pipeline → `Aggregate` → `Sink.Head` → `PipeTo(Self)`. No `Source.Queue`, no `ConcatMany`, no stream-in-stream. Everything inline in lambdas. No `CrawlContext`, no `FeedCrawlPlanner`. Same pattern as DownloadManager fan-out queries, just with more stages.

## Scope

**In scope:**
- Replace long-lived `Source.Queue` + `ConcatMany` with per-crawl short-lived `Source.Single` + `SelectMany` + `RunWith(Sink.Head)` + `PipeTo`
- Delete `CrawlContext.cs` — all methods inline in stream lambdas
- Delete `FeedCrawlPlanner.cs` — all logic inline in stream lambdas
- Delete `CrawlContextResponses` — response matching inline (error-as-data)
- All stage logic directly in lambdas, DI dependencies captured in closures
- Short-circuit (unchanged/failed) via `shortCircuit` closure + empty offsets array
- Probe + rulesets as the first `SelectAsync` in the stream, not as actor PipeTo

**Out of scope:**
- Changing persistence schema
- Changing FeedGate, RuleSetLookup, Telemetry
- Changing evaluation workers or FeedStoreManager
- Changing FeedOptions

## Approach

### Stream pattern

```
Source.Single(input)
  .SelectAsync(1, Prepare)         → int[] page offsets (or empty for short-circuit)
  .SelectMany(offsets => offsets)   ← flat fan-out, no ConcatMany
  .SelectAsync(3, FetchPage)       → PageEvaluation
  .TakeWhile + Where
  .SelectMany(items)               → MediathekItem
  .Where(lead filter)
  .StatefulSelectMany(dedup)
  .SelectMany(match + count)       → FeedMatch
  .GroupBy + GroupedWithin + Merge
  .SelectAsyncUnordered(Evaluate)  → EvalResult
  .Select(Gate)                    → GatedResult
  .GroupedWithin(batch)
  .SelectAsync(Publish)            → CrawlBatchResult
  .Aggregate(fold)                 → CrawlAccumulator
  .Select(toResult)                → CrawlResult
  .RunWith(Sink.Head)
  .PipeTo(Self)
```

Materialized per crawl in `StartCrawl()`. Runs once, produces one `CrawlResult`, completes. Same pattern as DownloadManager queries.

### Prepare stage (first SelectAsync)

One async step that does probe + rulesets + window computation. Returns page offsets or empty array for short-circuit. Sets closures for downstream stages (lookup, window, listTimestamp).

### Actor simplification

```
Tick → StartCrawl() → materialize stream
CrawlResult (PipeTo) → Persist → ScheduleNext
QueryFeedCrawlStatus → respond from state
```

No Queue, no OfferResult, no StreamCompleted/StreamFailed handlers. PipeTo failure handler produces CrawlResult.ForFailed.

### Files after refactor

| File | Content |
|------|---------|
| `FeedCrawlManager.cs` | Actor + stream, everything in one place |
| `CrawlResult.cs` | Records only: CrawlInput, CrawlBatchResult, CrawlAccumulator, CrawlResult, CrawlStatus, PageEvaluation, FeedMatch, EvalResult, GatedResult |
| `FeedCrawlManagerState.cs` | Persistence state (unchanged) |
| `FeedGate.cs` | Gate logic (own concern) |
| `RuleSetLookup.cs` | Lookup logic (own concern) |
| `Telemetry.cs` | Metrics (unchanged) |

### Deleted

- `CrawlContext.cs` — all methods inline in lambdas
- `FeedCrawlPlanner.cs` — all logic inline in lambdas
- `StreamSupervision.cs` — not needed for short-lived streams (PipeTo failure catches everything)
- `CrawlContextTests.cs` — CrawlContext gone
- `FeedCrawlPlannerTests.cs` — FeedCrawlPlanner gone

## Risks

- **Materializer cost per crawl**: Creating a new stream per crawl (every few minutes). DownloadManager does this per query (much more often). Negligible.
- **Larger lambdas**: The Prepare and FetchPage lambdas are 15-25 lines each. This is the FunkArr pattern — all other streams do the same.
- **Lost unit tests for EvaluatePage/Window**: These pure functions had dedicated unit tests. After inlining, they're covered by integration tests only. Acceptable — the integration tests already cover the edge cases through the real stream.
