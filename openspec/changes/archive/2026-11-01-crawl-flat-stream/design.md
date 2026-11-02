# Design: Flat Crawl Stream

## Context

FunkArr streams follow one pattern: flat, all logic inline, DI captured in closures. The FeedCrawlManager is the only exception — it uses a long-lived Source.Queue with ConcatMany (stream-in-stream) and delegates logic to CrawlContext/FeedCrawlPlanner helper classes. This design aligns the crawl with every other FunkArr stream.

## Goals / Non-Goals

**Goals:** One flat short-lived stream per crawl. No ConcatMany, no helper classes, no Source.Queue. Same pattern as DownloadManager fan-out queries.

**Non-Goals:** Changing persistence, FeedGate, RuleSetLookup, evaluation workers, FeedStoreManager.

## Decisions

### Per-crawl short-lived stream

The actor materializes a fresh stream for each crawl, not a long-lived queue. The stream starts with `Source.Single(input)`, fans out via `SelectMany(offsets)`, processes all pages through the flat pipeline, folds via `Aggregate`, and delivers one `CrawlResult` via `RunWith(Sink.Head) + PipeTo(Self)`.

### CrawlInput carries everything

```csharp
record CrawlInput(
    long Cursor,
    int PageSize,
    int MaxPages,
    int MaxConcurrency,
    double MinConfidence,
    long LeadLimit,
    TimeSpan InitialLookback,
    TimeSpan MaxCatchUp,
    long StartedAt);
```

Built by the actor from `_state.Cursor` + `_feedOptions` + `_timeProvider`. The stream never reads actor fields.

### Closure-based state in StartCrawl

```csharp
private void StartCrawl()
{
    if (_running) return;
    var feed = _feedOptions.CurrentValue;
    if (!feed.Enabled) { ScheduleNext(feed.Interval); return; }

    Timers.Cancel(_tickTimerKey);
    _running = true;

    var input = new CrawlInput(
        _state.Cursor, Math.Max(1, feed.PageSize), Math.Max(1, feed.MaxPages),
        feed.MaxConcurrency, feed.MinConfidence,
        (_timeProvider.GetUtcNow() + TimeSpan.FromDays(90)).ToUnixTimeSeconds(),
        feed.InitialLookback, feed.MaxCatchUp, _timeProvider.GetTimestamp());

    // All closures scoped to this one crawl
    RuleSetLookup? lookup = null;
    CrawlWindow? window = null;
    long listTimestamp = 0;
    CrawlResult? shortCircuit = null;
    var collected = 0;
    var matched = 0;
    string? pageError = null;

    Source.Single(input)
        .SelectAsync(1, async input => { /* prepare: probe + rulesets */ })
        .SelectMany(offsets => offsets)
        .SelectAsync(_pagePrefetch, async offset => { /* fetch + evaluate page */ })
        .TakeWhile(page => !page.ReachedEnd && page.Error is null, inclusive: true)
        .Select(page => { if (page.Error is not null) pageError = page.Error; return page; })
        .Where(page => page.Error is null)
        .SelectMany(page => page.Collected)
        .Where(item => item.Timestamp <= input.LeadLimit)
        .StatefulSelectMany(() =>
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            return item =>
            {
                var url = item.UrlVideoHd ?? item.UrlVideo ?? item.UrlVideoLow;
                return url is not null && seen.Add(FeedGate.StableIdFor(item.Channel, url))
                    ? [item] : [];
            };
        })
        .SelectMany(item =>
        {
            collected++;
            if (lookup!.Match(item.Topic) is not { } target) return [];
            matched++;
            return [new FeedMatch(item, target)];
        })
        .GroupBy(_maxSubstreams, match => match.Target.RuleSetId)
        .GroupedWithin(_groupSize, _groupWindow)
        .MergeSubstreams()
        .SelectAsyncUnordered(input.MaxConcurrency, async matches => { /* evaluate */ })
        .Select(eval => { /* gate */ })
        .GroupedWithin(_publishBatchSize, _publishWindow)
        .SelectAsync(1, async batch => { /* publish */ })
        .Aggregate(CrawlAccumulator.Empty, (acc, batch) => acc.Add(batch))
        .Select(acc => shortCircuit
            ?? (pageError is not null ? acc with { FirstError = acc.FirstError ?? pageError } : acc)
                .ToResult(listTimestamp, window?.Truncated ?? false, collected, matched,
                    _timeProvider.GetElapsedTime(input.StartedAt)))
        .RunWith(Sink.Head<CrawlResult>(), Context.Materializer())
        .PipeTo(Self,
            success: result => result,
            failure: ex => CrawlResult.ForFailed(listTimestamp, collected, collected - matched,
                0, false, _timeProvider.GetElapsedTime(input.StartedAt), ex.Message));
}
```

### Prepare stage (inline)

```csharp
.SelectAsync(1, async input =>
{
    try
    {
        var response = await _mediathek.Ask<QueryMediathekResponse>(
            new QueryMediathek([], "filmlisteTimestamp", "desc", Future: true, 0, 1, 300, null),
            _mediathekTimeout);

        if (response is not QueryMediathekCompleted probe)
        {
            shortCircuit = CrawlResult.ForFailed(0, 0, 0, 0, false,
                _timeProvider.GetElapsedTime(input.StartedAt),
                response is QueryMediathekError e ? e.Cause.Message : "queue full");
            return Array.Empty<int>();
        }

        if (probe.ListTimestamp <= 0 || probe.ListTimestamp <= input.Cursor)
        {
            shortCircuit = CrawlResult.ForUnchanged(probe.ListTimestamp,
                _timeProvider.GetElapsedTime(input.StartedAt));
            return Array.Empty<int>();
        }

        var rulesets = await _ruleSetResolver.Ask<RegisteredRuleSetsResult>(
            new QueryRegisteredRuleSets(), _ruleSetTimeout);

        listTimestamp = probe.ListTimestamp;
        lookup = RuleSetLookup.From(rulesets.Entries);

        var cursor = input.Cursor;
        var catchUp = (long)input.MaxCatchUp.TotalSeconds;
        window = cursor <= 0
            ? new CrawlWindow(Math.Max(0, listTimestamp - (long)input.InitialLookback.TotalSeconds), listTimestamp)
            : listTimestamp - catchUp is var earliest && cursor < earliest
                ? new CrawlWindow(earliest, listTimestamp, Truncated: true)
                : new CrawlWindow(cursor, listTimestamp);

        return Enumerable.Range(0, input.MaxPages).Select(p => p * input.PageSize).ToArray();
    }
    catch (Exception ex)
    {
        shortCircuit = CrawlResult.ForFailed(0, 0, 0, 0, false,
            _timeProvider.GetElapsedTime(input.StartedAt), ex.Message);
        return Array.Empty<int>();
    }
})
```

### Error handling

No StreamCompleted/StreamFailed handlers needed. The stream is short-lived — if it fails, `PipeTo` failure handler wraps it in `CrawlResult.ForFailed`. If any stage has an expected error, it's handled as data (error fields on records). Unexpected exceptions in the prepare stage are caught by the try/catch. Unexpected exceptions in pipeline stages are caught by PipeTo failure.

### No StreamSupervision needed

Short-lived streams don't need a LoggingDecider. An unexpected exception anywhere in the pipeline causes PipeTo to fire the failure handler, which produces a CrawlResult.ForFailed. The actor processes it like any other failure.

### HandleResult (unchanged from previous)

Maps CrawlResult directly to persistence events. Three-failure cursor advance logic inline.

### Records

Moved from CrawlContext.cs to CrawlResult.cs:
- `FeedMatch(MediathekItem Item, RuleSetTarget Target)` — stays as record
- `PageEvaluation` — moved from FeedCrawlPlanner.cs, with Error field

Kept in CrawlResult.cs:
- `CrawlInput`, `CrawlBatchResult`, `CrawlAccumulator`, `CrawlResult`, `CrawlStatus`
- `EvalResult`, `GatedResult`

Removed:
- `CrawlProbeResult` — not needed, prepare stage uses closures
- `CrawlStep` hierarchy — not needed, SelectMany + shortCircuit closure replaces branching
- `CrawlContext` — completely gone
- `Signal`, `OfferResult` — no queue

### CrawlWindow stays

`CrawlWindow(long LowerBound, long UpperBound, bool Truncated)` stays as a record — it's pure data, computed inline in the prepare stage and used by the fetch page stage.

## Risks / Trade-offs

- **Closure mutation safety**: Same as before — closures mutated in lambdas. Safe because the stream is materialized once and runs on one materializer thread. Even safer than the long-lived version because each crawl gets fresh closures.
- **Large StartCrawl method**: The entire stream is in one method (~120 lines). This matches the FunkArr pattern — the MvwManager's constructor is similar. If readability is a concern, `RunPipeline(input, ...)` can extract the pipeline portion as a private method returning `Source<CrawlResult>`, but it's not stream-in-stream — just method extraction.
- **PipeTo failure as catch-all**: If the materializer itself fails, PipeTo wraps it. This is less explicit than StreamCompleted/StreamFailed handlers but sufficient — the actor just needs a CrawlResult to persist and schedule.
