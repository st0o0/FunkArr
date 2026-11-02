# Proposal: Pure Crawl Stream

## Problem

The `FeedCrawlManager` stream is async orchestration disguised as a stream. Three nested `ConcatMany` levels, `Source.FromTask` for actor calls, WireTaps that Tell to Self for counter tracking, try/catch blocks inside SelectAsync, and mutable actor state (`_collected`, `_matched`, `_running`) that couples actor and stream. Njord's Akka.Streams pipelines show a cleaner pattern: the actor owns the stream lifecycle, the stream processes data purely, errors flow as data records, and no messages pass between stream and actor until the final result.

## Goal

Rewrite the crawl stream so it is a genuine data pipeline: `CrawlInput` in, `CrawlResult` out. The actor provides context (cursor, options, start timestamp) at offer time, the stream does all work (probe, rulesets, pages, evaluation, publish) and returns one `CrawlResult` record with everything (status, counts, errors, duration). No WireTaps, no Tell to Self, no actor counters, no try/catch — errors flow as data records through the pipeline.

## Scope

**In scope:**
- Flatten from 3 ConcatMany levels to 1 (at the page fan-out point)
- Replace `Signal` with `CrawlInput(cursor, options, startedAt)` — stream reads no actor state
- Replace `Source.FromTask(LoadContext)` with `SelectAsync` stages (probe, rulesets)
- Introduce error-as-data records: `FetchResult`, `EvalResult`, `GatedResult`, `BatchResult` all carry optional error
- Count collected/matched in closure captures inside the pipeline, not via actor counters
- Aggregate the entire crawl into one `CrawlResult` record
- Apply `WithAttributes(LoggingDecider)` — Resume on errors, drop bad elements
- Remove `_collected`, `_matched`, `_running` from actor
- Remove all WireTap/Tell/progress event handlers
- Remove `FeedCrawlOutcome` hierarchy (3 types) — replaced by `CrawlResult`
- Remove `FeedRunOutcome` — `CrawlResult` carries all fields directly

**Out of scope:**
- MergeHub/BroadcastHub patterns (Njord uses these for multi-producer/consumer — FunkArr has one producer)
- StreamRefs/SinkRefs (no cross-node streaming needed)
- Custom GraphStages
- Changing persistence schema or cursor semantics
- Changing evaluation workers or FeedStoreManager

## Approach

### Stream shape

```
Source.Queue<CrawlInput>(1, DropNew)
  .SelectAsync(1, Probe)               → ProbeResult { ListTimestamp, Error? }
  .SelectAsync(1, LoadOrSkip)           → CrawlStep = Ready(ctx) | Unchanged(ts) | Failed(err)
  .ConcatMany(step => step switch {     ← single fan-out point
      Ready   → RunPipeline(ctx),
      other   → Source.Single(CrawlResult)
  })
  .WithAttributes(LoggingDecider)
  .To(Sink.ActorRef<CrawlResult>(Self))
```

### RunPipeline (pure, no actor interaction except Ask for external services)

```
Source.From(ctx.PageOffsets)
  .SelectAsync(3, FetchPage)            → FetchResult { Items, ReachedEnd, Error? }
  .TakeWhile(!ReachedEnd, inclusive)
  .SelectMany(Items)
  .Where(WithinLead)
  .StatefulSelectMany(DistinctByStableId)
  .Select(item => { collected++; return item; })   // closure-captured counter
  .SelectMany(Classify)                              // match + count matched
  .GroupBy(ruleSetId) + GroupedWithin + Merge
  .SelectAsyncUnordered(Evaluate)       → EvalResult { Group, Items?, Error? }
  .Select(Gate)                         → GatedResult { Accepted, Error? }
  .GroupedWithin(batch)
  .SelectAsync(1, Publish)              → BatchResult { Groups, Failed, Published, Error? }
  .Aggregate(CrawlAccumulator.Empty, fold)
  .Select(acc => acc.ToResult(ctx, collected, matched))
```

### Key patterns (inspired by Njord)

1. **Errors as data**: Every async stage produces a result record with `Error?` field. No try/catch, no Recover. The Aggregate counts errors as FailedGroups.
2. **Closure-captured counting**: `collected` and `matched` are mutable locals in RunPipeline's closure. Safe because ConcatMany runs inner sources sequentially on one thread.
3. **CrawlInput carries context**: `CrawlInput(long Cursor, FeedOptions Options, long StartedAt)` — the stream never reads actor state.
4. **Duration in the stream**: StartedAt from CrawlInput + TimeProvider in final Select = Duration computed in the stream.
5. **LoggingDecider**: Resume on unexpected exceptions (defensive). Expected failures are already error-as-data and don't throw.

### Actor simplification

The actor becomes:
- **Tick** → build `CrawlInput(cursor, options, timestamp)` → `OfferAsync` → `PipeTo(Self, OfferResult)`
- **OfferResult** → if dropped, schedule next tick
- **CrawlResult** → `Decide` → `Persist` → `ScheduleNext`
- **QueryFeedCrawlStatus** → respond from `_state`
- **StreamCompleted/Failed** → crash (stream death)
- **Snapshot housekeeping** → unchanged

No counters, no running flag, no progress handlers, no outcome type switching. The `CrawlResult.Status` tells the actor everything.

### Records

| Record | Purpose |
|--------|---------|
| `CrawlInput(long Cursor, FeedOptions Options, long StartedAt)` | Queue element, replaces Signal |
| `ProbeResult(long ListTimestamp, string? Error)` | Probe output, error as data |
| `CrawlStep` abstract | Branch point: `Ready(CrawlContext)` / `Unchanged(long)` / `ProbeFailed(long, string)` |
| `FetchResult(MediathekItem[] Items, bool ReachedEnd, string? Error)` | Page fetch, error as data |
| `EvalResult(RuleSetGroup Group, SearchResultItem[]? Items, string? Error)` | Evaluation, error as data |
| `GatedResult(string RuleSetId, FeedEntry[] Accepted, string? Error)` | Gate output |
| `BatchResult(int Groups, int FailedGroups, int Published, string? Error)` | Publish batch |
| `CrawlAccumulator(int Groups, int FailedGroups, int Published, string? FirstError)` | Aggregate state |
| `CrawlResult(CrawlStatus, long ListTimestamp, int Collected, int Matched, int Groups, int FailedGroups, int Published, bool Gap, TimeSpan Duration, string? Error)` | Final — everything the actor needs |

### What goes away

- `Signal`, `ProbeResult` (current), `CrawlContext.ProbeQuery()` static
- `FeedCrawlOutcome` hierarchy (`FeedCrawlCompleted`, `FeedCrawlUnchanged`, `FeedCrawlFailed`)
- `FeedRunOutcome` + `FeedRunStatus` enum
- `FeedItemCollected`, `FeedItemMatched`, `FeedPageCrawled` progress events
- `OfferResult` private message (keep, renamed)
- `BatchResult` (current) — replaced with error-carrying version
- `CrawlTotals` — replaced by `CrawlAccumulator`
- Actor fields: `_collected`, `_matched`, `_running`, `_crawlStartedAt`
- Actor methods: `HandleCompleted`, `HandleUnchanged`, `HandleFailed`, `ResetCounters`, `Elapsed`
- `RecordTelemetry(FeedRunOutcome)` — replaced by `RecordTelemetry(CrawlResult)`

## Risks

- **Closure mutation safety**: Mutable counters in RunPipeline closures. Safe because ConcatMany runs inner sources on one materializer thread. Same pattern as Njord's FeatureEngine. Document this invariant.
- **LoggingDecider drops elements silently**: An unexpected exception in a stage drops that element and continues. This is correct for the crawl (one bad item shouldn't kill the whole crawl) but needs good logging in the decider.
- **CrawlInput captures options snapshot**: The stream uses the options from offer time, not current. This is actually better — options don't change mid-crawl.
