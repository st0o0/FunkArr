# Design: Feed Harvest Streams

## Actor Topology

```
FeedCrawlManager (persistent singleton, ~100 LOC)
│  Timer, Cursor persistence, Status, Options
│  No Become states — always idle
│
│  On Tick/StartFeedCrawl:
│    guard: if _harvesting → skip
│    _harvester.Tell(new StartHarvest(cursor, options snapshot))
│
│  On HarvestCompleted(summary):
│    persist cursor, record telemetry, _harvesting = false
│
│  On HarvestUnchanged:
│    record unchanged telemetry, _harvesting = false
│
│  On HarvestFailed(reason):
│    persist failure, _harvesting = false
│
└── FeedHarvestActor (long-lived child, ~180 LOC)
      Created once at RecoveryCompleted.
      Dependencies via Context.GetActor<T>() — no constructor passing.
      One Receive<StartHarvest> handler → builds and runs stream → PipeTo(Sender).
      No Become, no mutable state beyond _running guard (if needed).


FeedStoreManager (persistent singleton — unchanged)
```

## Stream Pipeline

The entire crawl run is a single materialized stream graph inside
FeedHarvestActor:

```
Source.Single(cursor)
  │
  SelectAsync(1)    Ask Mediathek (probe: is there new data?)
  Select            Extract timestamp or -1
  Where             Filter unchanged (→ empty stream → null result)
  │
  SelectAsync(1)    async: Ask RuleSetResolver, build Resolved record
  │
  ConcatMany ─────────────────────────────────────────────────────┐
  │                                                               │
  │  UnfoldAsync     Page loop: Ask Mediathek per page            │
  │                  FilterPage → items + reachedEnd              │
  │                  Sequential, stop at ReachedEnd or MaxPages   │
  │                                                               │
  │  SelectMany      GroupByTopic → ItemGroup[]                   │
  │                  Pure: topic → target matching                │
  │                                                               │
  │  SelectAsyncUnordered(4)                                      │
  │                  group.EvaluateAsync → ScoredGroup            │
  │                  Parallel: Ask TvEval / MovieEval             │
  │                                                               │
  │  Select          scored.Gate(minConfidence) → GatedResult     │
  │                  Pure: score check, ID check, rejections      │
  │                                                               │
  │  Aggregate       HarvestAccumulator.Add → collect all         │
  │                                                               │
  │  SelectAsync(1)  async: Publish to FeedStore if entries exist │
  │                                                               │
  └───────────────────────────────────────────────────────────────┘
  │
  Sink.FirstOrDefault   One HarvestAccumulator (or null if unchanged)
  │
  PipeTo(Sender)        HarvestCompleted | HarvestUnchanged | HarvestFailed
```

### Ask Pattern by Stage

| Stage | Pattern | Why |
|-------|---------|-----|
| Probe | `SelectAsync(Ask)` + `Select` + `Where` | Pure Ask, transform and filter are separate stages |
| Resolve | `SelectAsync(async { await Ask; return Resolved })` | Need to carry timestamp from input + Ask result |
| Page Loop | `UnfoldAsync(async { await Ask; ... })` | Signature requires async; sequential loop with termination |
| Evaluate | `SelectAsyncUnordered(group.EvaluateAsync)` | Genuine parallelism, async on the record |
| Publish | `SelectAsync(async { await Ask; return acc })` | Need to passthrough accumulator |

### Error Handling

- Ask timeouts fail the stream → PipeTo failure handler → HarvestFailed
- Individual page errors in UnfoldAsync fail the stream (acceptable —
  partial harvests are handled by the Manager's consecutive failure tracking
  and cursor-advance-on-repeated-failure logic)
- Individual evaluation failures could use Recover on the Ask Task inside
  EvaluateAsync to return a failed-group marker instead of failing the stream

## Types

### Stream Data Types (carry logic)

```
Resolved (nested in actor)
  Built by: Resolve SelectAsync stage
  Contains: timestamp, lower/upper bounds, truncated flag, target map,
            pageSize, maxPages, minConfidence
  Used by: ConcatMany lambda (page loop + grouping + gating config)

TopicEntry (nested in actor)
  Built by: BuildTargetMap private method
  Contains: ruleSetId, mediaType, externalIds

ItemGroup (own file)
  Built by: GroupByTopic
  Contains: ruleSetId, mediaType, externalIds, items[]
  Method: EvaluateAsync(eval, timeout) → Task<ScoredGroup>

ScoredGroup (own file)
  Built by: EvaluateAsync
  Contains: source group, scored results[]
  Method: Gate(minConfidence) → GatedResult

GatedResult (own file)
  Built by: Gate
  Contains: accepted FeedEntry[], seenCount, unmatchedCount, rejectedByReason

HarvestAccumulator (own file)
  Built by: Aggregate seed
  Contains: accepted entries list, totals, timestamp, truncated
  Methods: Add(GatedResult), ToSummary(duration), ToPublishCommand()

IngestSummary (own file)
  Built by: ToSummary
  Contains: status, timestamp, itemsFound, accepted, unmatched,
            truncated, duration, error
```

### Messages (public, in FunkArr.Messages)

```
Existing (unchanged):
  StartFeedCrawl
  QueryFeedCrawlStatus / FeedCrawlStatusResult
  PublishFeedEntries / PublishFeedEntriesCompleted/Failed
  QueryFeed / FeedResult

New (internal to Feed domain):
  StartHarvest(long Cursor, FeedOptions Options)
  HarvestCompleted(IngestSummary Summary)
  HarvestUnchanged
  HarvestFailed(string Reason)
```

### Actor Private Methods

FeedHarvestActor has private static methods for pure transformations
used inside the stream lambdas:

- `BuildTargetMap(entries)` — topic/alias → TopicEntry dictionary
- `FilterPage(response, ctx)` — window filter, URL check, dedup
- `GroupByTopic(items, targets)` — topic matching, grouping into ItemGroups
- `ComputeLower/ComputeTruncated` — window boundary computation

These are implementation details of the stream builder, not standalone helpers.

## File Structure

```
FunkArr.Feed/
├── Crawl/
│   ├── FeedCrawlManager.cs             ~100 LOC  Timer + Persist + Status
│   ├── FeedCrawlManagerState.cs         ~80 LOC  Cursor, failures (updated)
│   ├── FeedHarvestActor.cs             ~180 LOC  Stream builder + private helpers
│   │     nested: Resolved, TopicEntry
│   ├── ItemGroup.cs                     ~30 LOC  EvaluateAsync
│   ├── ScoredGroup.cs                   ~50 LOC  Gate
│   ├── GatedResult.cs                    ~5 LOC  Value type
│   ├── HarvestAccumulator.cs            ~50 LOC  Fold state + ToSummary
│   └── IngestSummary.cs                 ~10 LOC  Result record
│
├── Store/
│   ├── FeedStoreManager.cs              unchanged
│   └── FeedStoreManagerState.cs         unchanged
│
├── PersistenceMapping.cs                unchanged
└── Telemetry.cs                         updated
```

### Deleted Files

- `FeedCrawlPageActor.cs`
- `FeedCrawlPageActorState.cs`
- `FeedCrawlPageMessages.cs`
- `FeedCrawlRunState.cs`
- `CrawlResult.cs` (replaced by IngestSummary)
- `RuleSetTarget.cs` (replaced by TopicEntry nested in actor)
