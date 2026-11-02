# Design: Crawl Manager Rewrite

## Context

The `FeedCrawlManager` has a long-lived Akka.Streams pipeline but runs probe and ruleset loading outside the stream as `Ask`/`PipeTo` chains. This design moves everything into the stream, eliminating `FeedCrawlPlan`, `FeedCrawlRun`, and `RunId`.

## Goals / Non-Goals

**Goals:** A fully stream-centric crawl manager where the actor is a thin shell (timer, queue offer, persistence, telemetry counters). Every step from probe through publish lives in the stream pipeline.

**Non-Goals:** Changing the inner pipeline logic (page fetching, dedup, matching, evaluation, gating, publishing). Changing persistence schema or cursor semantics. Changing `FeedStoreManager` or evaluation worker interaction.

## Decisions

### Stream topology

The stream has three layers: signal dispatch (probe + branch), context setup (rulesets), and the crawl pipeline (pages → publish).

```
Source.Queue<Signal>(1, DropNew)
  .SelectAsync(1, _ => Probe())
  .ConcatMany(probe => Dispatch(probe))
  .To(Sink.ActorRef<FeedCrawlOutcome>(Self, StreamCompleted, StreamFailed))
```

`Dispatch` branches on probe result:

```csharp
Source<FeedCrawlOutcome, NotUsed> Dispatch(ProbeResult probe)
{
    if (!FeedCrawlPlanner.HasUpdate(_state.Cursor, probe.ListTimestamp))
        return Source.Single<FeedCrawlOutcome>(
            new FeedCrawlUnchanged(probe.ListTimestamp));

    return Source.FromTask(LoadContext(probe.ListTimestamp))
        .ConcatMany(ctx => RunCrawl(ctx))
        .Recover(ex => new FeedCrawlFailed(ex.Message));
}
```

`LoadContext` asks the ruleset resolver and builds a `CrawlContext`. `RunCrawl` is the existing inner pipeline with an `Aggregate` at the end instead of per-event tracking.

### Records at stage boundaries

**New records** (internal to FunkArr.Feed):

| Record | Purpose | Built by |
|--------|---------|----------|
| `Signal` | Unit type offered to queue on tick | Actor |
| `ProbeResult(long ListTimestamp)` | Probe output | `Probe()` async lambda |
| `CrawlContext(long ListTimestamp, CrawlWindow Window, RuleSetLookup Lookup, int PageSize, int MaxPages, double MinConfidence, long LeadLimit)` | Replaces FeedCrawlPlan. Immutable crawl parameters, built inside stream. | `LoadContext()` |
| `BatchResult(int Groups, int FailedGroups, int Published)` | Single publish batch result | `SelectAsync(1, Publish)` |
| `CrawlTotals(int Groups, int FailedGroups, int Published)` | Aggregate seed/result, folded from BatchResults | `Aggregate()` |

**Outcome hierarchy** (replaces FeedCrawlEvent):

```csharp
internal abstract record FeedCrawlOutcome;
internal sealed record FeedCrawlCompleted(
    long ListTimestamp, int Published, int FailedGroups, bool Gap) : FeedCrawlOutcome;
internal sealed record FeedCrawlUnchanged(long ListTimestamp) : FeedCrawlOutcome;
internal sealed record FeedCrawlFailed(string Error) : FeedCrawlOutcome;
```

**Progress events** (WireTap → Self, for telemetry only):

```csharp
internal sealed record FeedItemCollected;
internal sealed record FeedItemMatched;
internal sealed record FeedPageCrawled(bool ReachedEnd);
```

No `RunId` on any of these — the queue capacity of 1 with DropNew ensures one crawl at a time. Stream ordering guarantees all progress events arrive before the outcome.

### Existing records that stay unchanged

`PageEvaluation`, `MediathekItem`, `FeedMatch`, `FeedEvaluation`, `FeedGated`, `RuleSetGroup`, `CrawlWindow`, `FeedEntry` — the inner pipeline is untouched.

### CrawlContext methods

`CrawlContext` carries the same helper methods that `FeedCrawlPlan` had (minus RunId-related ones):

- `PageOffsets` — enumerable of page offsets
- `PageQuery(offset)` — builds `QueryMediathek`
- `Read(response)` — delegates to `FeedCrawlPlanner.EvaluatePage`
- `WithinLead(item)` — timestamp filter
- `Match(item)` — ruleset lookup
- `Group(matches)` — builds `RuleSetGroup`
- `EvaluateSeries(group)` / `EvaluateMovies(group)` — evaluation message factories

The static `Query(offset, size)` method stays on `FeedCrawlPlanner` (it doesn't need context).

### Actor state during a crawl

```csharp
private int _collected;
private int _matched;
```

Reset when any `FeedCrawlOutcome` arrives. `HandleCompleted` combines the stream's `Published`/`FailedGroups`/`Gap` with `_collected`/`_matched` to build the persistence event.

### RunCrawl pipeline

```csharp
Source<FeedCrawlOutcome, NotUsed> RunCrawl(CrawlContext ctx)
{
    var groups = Source.From(ctx.PageOffsets)
        .SelectAsync(_pagePrefetch, async offset =>
            ctx.Read(await _mediathek.Ask<QueryMediathekResponse>(
                ctx.PageQuery(offset), _mediathekTimeout)))
        .WireTap(page => _self.Tell(new FeedPageCrawled(page.ReachedEnd)))
        .TakeWhile(page => !page.ReachedEnd, inclusive: true)
        .SelectMany(page => page.Collected)
        .Where(ctx.WithinLead)
        .StatefulSelectMany(FeedCrawlPlanner.DistinctByStableId)
        .WireTap(_ => { _self.Tell(new FeedItemCollected()); Telemetry.ItemsSeen.Add(1); })
        .SelectMany(ctx.Match)
        .WireTap(_ => _self.Tell(new FeedItemMatched()))
        .GroupBy(_maxSubstreams, match => match.Target.RuleSetId)
        .GroupedWithin(_groupSize, _groupWindow)
        .MergeSubstreams();

    return groups
        .SelectAsyncUnordered(_feedOptions.CurrentValue.MaxConcurrency, async matches =>
        {
            var group = CrawlContext.Group(matches);
            try
            {
                return FeedEvaluation.Of(group, group.MediaType == MediaType.Movie
                    ? (await _movieEvaluation.Ask<EvaluateMoviesResponse>(
                        CrawlContext.EvaluateMovies(group), _evaluationTimeout)).ItemsOrThrow()
                    : (await _tvEvaluation.Ask<EvaluateSeriesResponse>(
                        CrawlContext.EvaluateSeries(group), _evaluationTimeout)).ItemsOrThrow());
            }
            catch (Exception ex)
            {
                return FeedEvaluation.Failed(group, ex);
            }
        })
        .Select(evaluation => evaluation.Gate(ctx.MinConfidence))
        .GroupedWithin(_publishBatchSize, _publishWindow)
        .SelectAsync(1, async batch =>
        {
            var gated = batch.ToArray();
            var entries = gated.SelectMany(g => g.Accepted).ToArray();
            try
            {
                var published = entries.Length == 0
                    ? 0
                    : (await _store.Ask<PublishFeedEntriesResponse>(
                        new PublishFeedEntries(entries), _publishTimeout)).PublishedOrThrow();
                return new BatchResult(gated.Length, gated.Count(g => g.Error is not null), published);
            }
            catch (Exception ex)
            {
                return new BatchResult(gated.Length, gated.Length, 0);
            }
        })
        .Aggregate(CrawlTotals.Empty, (totals, batch) => totals.Add(batch))
        .Select(totals => (FeedCrawlOutcome)new FeedCrawlCompleted(
            ctx.ListTimestamp, totals.Published, totals.FailedGroups,
            ctx.Window.Truncated))
        .Concat(Source.Single<FeedCrawlOutcome>(
            new FeedCrawlCompleted(ctx.ListTimestamp, 0, 0, ctx.Window.Truncated)))
        .Take(1);
}
```

Note: `Aggregate` emits when the source completes. For an empty pipeline (no pages matched the window), it emits `CrawlTotals.Empty`, which produces a `FeedCrawlCompleted` with zeros. The `Concat` + `Take(1)` is a safety net but `Aggregate` always emits the seed on empty sources, so the `Concat`/`Take` may be unnecessary — verify during implementation.

### Enabled check

The actor checks `FeedOptions.Enabled` before offering `Signal` to the queue. This stays outside the stream — it's a configuration concern, not a pipeline concern.

### Error handling

| Failure | Current | After rewrite |
|---------|---------|---------------|
| Probe fails | `PipeTo` failure → `Probed(runId, Error)` → `Finish(Failed)` | Exception in `SelectAsync` → `Recover` in `Dispatch` → `FeedCrawlFailed` |
| RuleSet load fails | `PipeTo` failure → `RuleSetsFailed` → `Finish(Failed)` | Exception in `FromTask(LoadContext)` → `Recover` in `Dispatch` → `FeedCrawlFailed` |
| Page fetch fails | Exception in stream → `Recover` → `FeedCrawlFailed` | Same — `Recover` in `RunCrawl` |
| Evaluation fails | Try/catch → `FeedEvaluation.Failed` | Same |
| Publish fails | Try/catch → `FeedBatchPublishFailed` | Try/catch → `BatchResult` with all groups failed |
| Stream dies | `CrawlStreamCompleted/Failed` → crash actor | Same |

### Telemetry

WireTap-based telemetry stays in the stream. Progress events (`FeedItemCollected`, `FeedItemMatched`, `FeedPageCrawled`) sent to actor for counter tracking. Outcome-level telemetry (`Telemetry.Crawls`, `Telemetry.CrawlDuration`, etc.) recorded by the actor when the outcome arrives, same as today.

Rejected-reason telemetry stays inside `FeedEvaluation.Gate()` — unchanged.

## Risks / Trade-offs

- **Dispatch nesting** (ConcatMany → FromTask → ConcatMany): Akka.Streams handles nested sources correctly. Inner sources complete before outer ConcatMany pulls the next element. Error messages from inner failures may be less obvious — the Recover at Dispatch level catches everything.
- **Counter drift on actor restart**: If the actor restarts mid-crawl, `_collected`/`_matched` reset to 0. The stream is also re-materialized, so the partial crawl's progress is lost. Next tick starts a fresh crawl. Acceptable — same behavior as today (the `_run` was also in-memory only).
- **Aggregate on empty source**: Akka.Streams `Aggregate` emits the seed when upstream completes with zero elements. Verify this during implementation — if not, add an `OrElse` fallback.
