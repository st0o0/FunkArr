# Design: Pure Crawl Stream

## Context

The previous rewrite moved probe and rulesets into the stream but kept async orchestration patterns (nested ConcatMany, Source.FromTask, WireTap → Tell, actor counters). Njord's Akka.Streams patterns show a cleaner separation: actor owns lifecycle, stream processes data purely, errors flow as records.

## Goals / Non-Goals

**Goals:** A genuine data pipeline where `CrawlInput` goes in and `CrawlResult` comes out. No actor-stream coupling beyond Sink.ActorRef. Errors as data. Counting in closures.

**Non-Goals:** Hub patterns, StreamRefs, custom GraphStages. These are Njord patterns for multi-producer/consumer scenarios that don't apply here.

## Decisions

### One ConcatMany at the natural fan-out

The stream has three phases: setup (1:1), branch (1:1), and pipeline (1:many). Only the pipeline needs ConcatMany:

```csharp
_crawls = Source.Queue<CrawlInput>(1, OverflowStrategy.DropNew)
    .SelectAsync(1, Probe)
    .SelectAsync(1, LoadOrSkip)
    .Via(Flow.Create<CrawlStep>().ConcatMany(Run))
    .WithAttributes(StreamSupervision.LoggingDecider(_log))
    .To(Sink.ActorRef<CrawlResult>(_self, new CrawlStreamCompleted(), ex => new CrawlStreamFailed(ex)))
    .Run(Context.Materializer());
```

`Probe` and `LoadOrSkip` are plain `SelectAsync` stages — not Source.FromTask, not ConcatMany. They transform one record into another.

### Probe stage

```csharp
async Task<ProbeResult> Probe(CrawlInput input)
{
    try
    {
        var response = await _mediathek.Ask<QueryMediathekResponse>(
            CrawlContext.ProbeQuery(), _mediathekTimeout);
        return response switch
        {
            QueryMediathekCompleted c => new ProbeResult(input, c.ListTimestamp, null),
            QueryMediathekQueueFull => new ProbeResult(input, 0, "MediathekViewWeb queue full"),
            QueryMediathekError e => new ProbeResult(input, 0, $"MediathekViewWeb query failed: {e.Cause.Message}"),
            _ => new ProbeResult(input, 0, "unexpected probe response")
        };
    }
    catch (Exception ex)
    {
        return new ProbeResult(input, 0, ex.Message);
    }
}
```

Error is a field, not an exception. The `ProbeResult` carries the original `CrawlInput` forward (cursor, options, startedAt).

### LoadOrSkip stage

```csharp
async Task<CrawlStep> LoadOrSkip(ProbeResult probe)
{
    if (probe.Error is not null)
        return new CrawlFailed(probe.Input, probe.ListTimestamp, probe.Error);

    if (!FeedCrawlPlanner.HasUpdate(probe.Input.Cursor, probe.ListTimestamp))
        return new CrawlUnchanged(probe.Input, probe.ListTimestamp);

    try
    {
        var result = await _ruleSetResolver.Ask<RegisteredRuleSetsResult>(
            new QueryRegisteredRuleSets(), _ruleSetTimeout);
        var opts = probe.Input.Options;
        var window = FeedCrawlPlanner.Window(probe.Input.Cursor, probe.ListTimestamp,
            opts.InitialLookback, opts.MaxCatchUp);
        var ctx = CrawlContext.Create(probe.ListTimestamp, window,
            RuleSetLookup.From(result.Entries), opts, _timeProvider.GetUtcNow());
        return new CrawlReady(probe.Input, ctx);
    }
    catch (Exception ex)
    {
        return new CrawlFailed(probe.Input, probe.ListTimestamp, $"ruleset lookup failed: {ex.Message}");
    }
}
```

### CrawlStep hierarchy

```csharp
abstract record CrawlStep(CrawlInput Input);
sealed record CrawlReady(CrawlInput Input, CrawlContext Context) : CrawlStep(Input);
sealed record CrawlUnchanged(CrawlInput Input, long ListTimestamp) : CrawlStep(Input);
sealed record CrawlFailed(CrawlInput Input, long ListTimestamp, string Error) : CrawlStep(Input);
```

Every variant carries `CrawlInput` so the final `CrawlResult` can compute Duration from `Input.StartedAt`.

### Run (single ConcatMany)

```csharp
Source<CrawlResult, NotUsed> Run(CrawlStep step) => step switch
{
    CrawlReady ready => RunPipeline(ready.Input, ready.Context),
    CrawlUnchanged unchanged => Source.Single(CrawlResult.ForUnchanged(
        unchanged.ListTimestamp, Elapsed(unchanged.Input))),
    CrawlFailed failed => Source.Single(CrawlResult.ForFailed(
        failed.ListTimestamp, 0, 0, 0, false, Elapsed(failed.Input), failed.Error)),
    _ => Source.Empty<CrawlResult>()
};
```

### RunPipeline — pure data pipeline with closure counting

```csharp
Source<CrawlResult, NotUsed> RunPipeline(CrawlInput input, CrawlContext ctx)
{
    var collected = 0;
    var matched = 0;

    return Source.From(ctx.PageOffsets)
        .SelectAsync(_pagePrefetch, async offset =>
        {
            try
            {
                var response = await _mediathek.Ask<QueryMediathekResponse>(
                    ctx.PageQuery(offset), _mediathekTimeout);
                return ctx.Read(response);
            }
            catch (Exception ex)
            {
                return PageEvaluation.Failed(ex.Message);
            }
        })
        .TakeWhile(page => !page.ReachedEnd && page.Error is null, inclusive: true)
        .Where(page => page.Error is null)
        .SelectMany(page => page.Collected)
        .Where(ctx.WithinLead)
        .StatefulSelectMany(FeedCrawlPlanner.DistinctByStableId)
        .Select(item => { collected++; return item; })
        .SelectMany(item =>
        {
            if (ctx.Match(item) is not { } match) return [];
            matched++;
            return [match];
        })
        .GroupBy(_maxSubstreams, match => match.Target.RuleSetId)
        .GroupedWithin(_groupSize, _groupWindow)
        .MergeSubstreams()
        .SelectAsyncUnordered(input.Options.MaxConcurrency, async matches =>
        {
            var group = CrawlContext.Group(matches);
            try
            {
                var items = group.MediaType == MediaType.Movie
                    ? (await _movieEvaluation.Ask<EvaluateMoviesResponse>(
                        CrawlContext.EvaluateMovies(group), _evaluationTimeout)).ItemsOrThrow()
                    : (await _tvEvaluation.Ask<EvaluateSeriesResponse>(
                        CrawlContext.EvaluateSeries(group), _evaluationTimeout)).ItemsOrThrow();
                return new EvalResult(group, items, null);
            }
            catch (Exception ex)
            {
                return new EvalResult(group, null, ex.Message);
            }
        })
        .Select(eval => eval.Gate(ctx.MinConfidence))
        .GroupedWithin(_publishBatchSize, _publishWindow)
        .SelectAsync(1, async batch =>
        {
            var gated = batch.ToArray();
            var entries = gated.Where(g => g.Error is null).SelectMany(g => g.Accepted).ToArray();
            var failedGroups = gated.Count(g => g.Error is not null);
            try
            {
                var published = entries.Length == 0
                    ? 0
                    : (await _store.Ask<PublishFeedEntriesResponse>(
                        new PublishFeedEntries(entries), _publishTimeout)).PublishedOrThrow();
                return new BatchResult(gated.Length, failedGroups, published, null);
            }
            catch (Exception ex)
            {
                return new BatchResult(gated.Length, gated.Length, 0, ex.Message);
            }
        })
        .Aggregate(CrawlAccumulator.Empty, (acc, batch) => acc.Add(batch))
        .Select(acc => acc.ToResult(ctx.ListTimestamp, ctx.Window.Truncated,
            collected, matched, Elapsed(input)));
}
```

### CrawlAccumulator

```csharp
sealed record CrawlAccumulator(int Groups, int FailedGroups, int Published, string? FirstError)
{
    internal static readonly CrawlAccumulator Empty = new(0, 0, 0, null);

    internal CrawlAccumulator Add(BatchResult batch) => new(
        Groups + batch.Groups,
        FailedGroups + batch.FailedGroups,
        Published + batch.Published,
        FirstError ?? batch.Error);

    internal CrawlResult ToResult(long listTimestamp, bool truncated,
        int collected, int matched, TimeSpan duration)
    {
        var unmatched = collected - matched;
        if (FailedGroups > 0)
            return CrawlResult.ForFailed(listTimestamp, collected, unmatched, Published, truncated, duration,
                $"{FailedGroups} of {Groups} ruleset groups failed");
        if (FirstError is not null)
            return CrawlResult.ForFailed(listTimestamp, collected, unmatched, Published, truncated, duration, FirstError);
        return CrawlResult.ForCompleted(listTimestamp, collected, unmatched, Published, truncated, duration);
    }
}
```

### CrawlResult

```csharp
enum CrawlStatus { Completed, Unchanged, Failed }

sealed record CrawlResult(
    CrawlStatus Status,
    long ListTimestamp,
    int NewItems,
    int Published,
    int Unmatched,
    bool Gap,
    TimeSpan Duration,
    string? Error)
{
    internal static CrawlResult ForCompleted(long ts, int items, int unmatched, int published,
        bool gap, TimeSpan duration) =>
        new(CrawlStatus.Completed, ts, items, published, unmatched, gap, duration, null);

    internal static CrawlResult ForFailed(long ts, int items, int unmatched, int published,
        bool gap, TimeSpan duration, string error) =>
        new(CrawlStatus.Failed, ts, items, published, unmatched, gap, duration, error);

    internal static CrawlResult ForUnchanged(long ts, TimeSpan duration) =>
        new(CrawlStatus.Unchanged, ts, 0, 0, 0, false, duration, null);
}
```

### Actor (simplified)

```csharp
Command<Tick>(_ => StartCrawl());
Command<StartFeedCrawl>(_ => StartCrawl());
Command<QueryFeedCrawlStatus>(_ => Sender.Tell(_state.ToStatus(feed.Enabled, /* running? */)));
Command<OfferResult>(msg => { if (not Enqueued) ScheduleNext(); });
Command<CrawlResult>(HandleResult);
Command<CrawlStreamCompleted>(_ => throw ...);
Command<CrawlStreamFailed>(msg => throw ...);
// + snapshot housekeeping
```

`HandleResult` maps `CrawlResult` directly to persistence decisions — no intermediate `FeedRunOutcome`:

```csharp
void HandleResult(CrawlResult result)
{
    RecordTelemetry(result);
    var now = _timeProvider.GetUtcNow();
    switch (result.Status)
    {
        case CrawlStatus.Completed:
            Persist(new FeedCursorAdvanced(result.ListTimestamp, now, result.NewItems,
                result.Published, result.Unmatched, result.Gap), ...);
            break;
        case CrawlStatus.Failed:
            // three-failure cursor advance rule
            var failures = _state.ConsecutiveFailures + 1;
            if (failures >= 3 && result.ListTimestamp > _state.Cursor)
                Persist(new FeedCursorAdvanced(result.ListTimestamp, now, result.NewItems,
                    result.Published, result.Unmatched, Gap: true), ...);
            else
                Persist(new FeedRunFailed(now, result.Error!, failures), ...);
            break;
        case CrawlStatus.Unchanged:
            _state = _state.WithUnchangedRun(now);
            break;
    }
    ScheduleNext(feed.Interval);
}
```

### Running detection

The actor needs to know if a crawl is running for `QueryFeedCrawlStatus`. Two options:

**Option A**: Track via offer/result cycle — set a bool on successful offer, clear on CrawlResult.
**Option B**: Just check if the queue accepted the last offer.

Option A is simpler and explicit. One `_running` bool, set in StartCrawl on successful offer (in OfferResult handler when Enqueued), cleared in HandleResult. Minimal coupling.

### LoggingDecider

```csharp
internal static class StreamSupervision
{
    internal static Attributes LoggingDecider(ILoggingAdapter log) =>
        ActorAttributes.CreateSupervisionStrategy(ex =>
        {
            log.Warning(ex, "Feed crawl stream element dropped");
            return Directive.Resume;
        });
}
```

Applied at the outer level. Expected errors are already handled as data records in SelectAsync stages. The LoggingDecider catches unexpected exceptions (serialization errors, null refs, etc.) and drops the element instead of killing the stream.

### PageEvaluation with error

Add `Error?` field:
```csharp
sealed record PageEvaluation(MediathekItem[] Collected, bool ReachedEnd, int InProgress, string? Error = null)
{
    internal static PageEvaluation Failed(string error) => new([], true, 0, error);
}
```

## Risks / Trade-offs

- **Closure mutation in RunPipeline**: `collected++` and `matched++` in lambdas. Safe because ConcatMany runs inner sources sequentially — same pattern as Njord's FeatureEngine. If this stream were ever materialized more than once or shared, it would break. Document as invariant.
- **LoggingDecider drops silently**: An unexpected error drops one element. For items this is fine (one missed entry). For a batch publish this could mean a whole batch is lost. Mitigated by: expected errors are already data records, only truly unexpected errors hit the decider.
- **try/catch still exists in SelectAsync**: The "errors as data" pattern still uses try/catch inside SelectAsync lambdas — the difference is that exceptions become record fields instead of stream failures. This is the same pattern Njord uses.
- **FeedCrawlManagerState.Decide() goes away**: The decision logic moves inline into HandleResult. This is simpler (no intermediate FeedRunOutcome) but duplicates the three-failure rule. Keep it clean.
