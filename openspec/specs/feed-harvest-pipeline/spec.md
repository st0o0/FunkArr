# Feed Harvest Pipeline

Defines the Akka.Streams pipeline inside FeedHarvestActor.

## Stream Graph

```
Source.Single(cursor)
  │
  ├─ SelectAsync(1)         Probe: Ask Mediathek for listTimestamp
  ├─ Select                  Extract timestamp or -1 (unchanged)
  ├─ Where(ts > 0)           Short-circuit if unchanged → empty stream
  │
  ├─ SelectAsync(1, async)   Resolve: Ask RuleSetResolver, build Resolved
  │
  ├─ ConcatMany(ctx =>       Harvest scope: ctx (Resolved) available
  │   │
  │   ├─ UnfoldAsync          Page loop (sequential)
  │   │   Ask Mediathek per page, FilterPage, stop on ReachedEnd/MaxPages
  │   │
  │   ├─ SelectMany           GroupByTopic → ItemGroup[]
  │   │
  │   ├─ SelectAsyncUnordered(4)  Evaluate per group
  │   │   group.EvaluateAsync → ScoredGroup
  │   │
  │   ├─ Select               scored.Gate(minConfidence) → GatedResult
  │   │
  │   ├─ Aggregate            Collect into HarvestAccumulator
  │   │
  │   └─ SelectAsync(1, async)  Publish to FeedStore
  │  )
  │
  ├─ Sink.FirstOrDefault     One result or null
  └─ PipeTo(Sender)          HarvestCompleted | HarvestUnchanged | HarvestFailed
```

## Ask Patterns

| Stage | Pattern | Rationale |
|-------|---------|-----------|
| Probe | `.SelectAsync(1, _ => actor.Ask(...))` then `.Select(transform)` then `.Where(filter)` | Pure Ask, transformation and filtering are separate stages |
| Resolve | `.SelectAsync(1, async ts => { var r = await Ask; return new Resolved(ts, r); })` | Need to combine input (timestamp) with Ask result |
| Page fetch | `UnfoldAsync(async offset => { var r = await Ask; ... })` | Async required by UnfoldAsync signature; sequential loop |
| Evaluate | `.SelectAsyncUnordered(4, group => group.EvaluateAsync(eval, timeout))` | Genuine parallelism across independent groups |
| Publish | `.SelectAsync(1, async acc => { if (acc.HasEntries) await Ask; return acc; })` | Conditional publish, passthrough accumulator |

## Sequential Page Fetch

`Source.UnfoldAsync` with offset state:
- Start at offset 0
- Each iteration: Ask Mediathek, filter page, decide next
- Stop when: ReachedEnd (item timestamp <= lower bound), or MaxPages reached
- Offset set to -1 signals termination (next iteration returns None)
- Sequential by design — MVW Manager's internal queue (maxConcurrent=3)
  handles HTTP parallelism

## ConcatMany Scope

The `ConcatMany` lambda receives `Resolved` (the probe + resolve result)
and builds the inner source. All stages inside ConcatMany have access to
`ctx` for:
- Window bounds (lower, upper) used in FilterPage
- Target map used in GroupByTopic
- MinConfidence used in Gate
- PageSize, MaxPages used in UnfoldAsync

The inner source's Aggregate + Publish produce one HarvestAccumulator per
run. ConcatMany emits it, Sink.FirstOrDefault captures it.

## Unchanged Handling

When probe returns unchanged (timestamp <= cursor), the `Where(ts > 0)`
filter drops the element. ConcatMany receives nothing. The stream completes
empty. `Sink.FirstOrDefault` returns null. PipeTo maps null → HarvestUnchanged.

## Error Handling

- Ask timeout → stream failure → PipeTo failure → HarvestFailed
- MVW query error (QueryMediathekError/Queued) → mapped to -1 in probe
  Select → treated as unchanged, or fails the UnfoldAsync
- Individual eval failures: EvaluateAsync can use `.Recover()` on the Ask
  Task to return a failed-group marker instead of failing the stream
  (to be decided during implementation)
