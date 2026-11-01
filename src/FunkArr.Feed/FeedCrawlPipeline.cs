using System.Collections.Immutable;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Feed;

internal sealed record RunningCrawl(UniqueKillSwitch KillSwitch, CancellationTokenSource Cancellation, Task<FeedRunOutcome> Completion)
{
    internal void Cancel()
    {
        KillSwitch.Abort(new OperationCanceledException("Feed crawl cancelled"));
        Cancellation.Cancel();
    }
}

internal sealed class FeedCrawlPipeline(FeedCrawlStages stages, TimeProvider timeProvider)
{
    internal const int PagePrefetch = 3;
    internal const int PublishBatchSize = 100;
    internal const int MaxQueueRetries = 2;

    private static readonly TimeSpan _queueRetryDelay = TimeSpan.FromMilliseconds(500);

    internal RunningCrawl Run(long cursor, FeedOptions options, IMaterializer materializer)
    {
        var started = timeProvider.GetTimestamp();
        var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;

        var (killSwitch, stats) = Source.Single(cursor)
            .SelectAsync(1, c => Probe(c, options, ct))
            .SelectAsync(1, plan => plan.Probed.HasUpdate ? WithRuleSets(plan, ct) : Task.FromResult(plan))
            .ConcatMany(plan => plan.Probed.HasUpdate
                ? Crawl(plan, options, ct)
                : Source.Single<CrawlStep>(plan.Probed))
            .Recover(ex => new Option<CrawlStep>(new CrawlFailed(ex.Message)))
            .ViaMaterialized(KillSwitches.Single<CrawlStep>(), Keep.Right)
            .ToMaterialized(Sink.Aggregate<CrawlStep, FeedRunStats>(FeedRunStats.Empty, (s, step) => s.Add(step)), Keep.Both)
            .Run(materializer);

        return new RunningCrawl(killSwitch, cancellation, Complete(stats, started));
    }

    private Source<CrawlStep, NotUsed> Crawl(CrawlPlan plan, FeedOptions options, CancellationToken ct) =>
        Source.Single<CrawlStep>(plan.Probed)
            .Concat(Source.From(PageOffsets(options))
                .SelectAsync(PagePrefetch, offset => QueryItems(offset, options.PageSize, ct))
                .Select(items => FeedCrawlPlanner.EvaluatePage(items, plan.Window, options.PageSize))
                .TakeWhile(page => !page.ReachedEnd, inclusive: true)
                .StatefulSelectMany(FeedCrawlPlanner.DistinctAcrossPages)
                .SelectMany(page => Split(page, plan))
                .SelectAsyncUnordered(options.MaxConcurrency, step => step is GroupPending pending
                    ? EvaluateGroup(pending.Group, options.MinConfidence, ct)
                    : Task.FromResult(step))
                .Batch(PublishBatchSize, step => ImmutableList.Create(step), (batch, step) => batch.Add(step))
                .SelectAsync(1, batch => PublishBatch(batch, ct))
                .SelectMany(steps => steps));

    private async Task<CrawlPlan> Probe(long cursor, FeedOptions options, CancellationToken ct)
    {
        var probe = await Query(0, 1, ct);
        var listTimestamp = probe.ListTimestamp;
        var hasUpdate = FeedCrawlPlanner.HasUpdate(cursor, listTimestamp);
        var window = FeedCrawlPlanner.Window(cursor, listTimestamp, options.InitialLookback, options.MaxCatchUp);

        return new CrawlPlan(new CrawlProbed(listTimestamp, hasUpdate, hasUpdate && window.Truncated), window, RuleSetLookup.Empty);
    }

    private async Task<CrawlPlan> WithRuleSets(CrawlPlan plan, CancellationToken ct) =>
        plan with { Lookup = RuleSetLookup.From(await stages.LoadRuleSets(ct)) };

    private IEnumerable<CrawlStep> Split(PageEvaluation page, CrawlPlan plan)
    {
        var items = FeedCrawlPlanner.WithinLead(page.Collected, timeProvider.GetUtcNow());
        var grouping = plan.Lookup.Group(items);

        return grouping.Groups
            .Select(group => (CrawlStep)new GroupPending(group))
            .Prepend(new PageCollected(items.Length, grouping.Unmatched, page.ReachedEnd));
    }

    private async Task<CrawlStep> EvaluateGroup(RuleSetGroup group, double minConfidence, CancellationToken ct)
    {
        try
        {
            var results = await stages.Evaluate(group, ct);
            var gate = FeedGate.Evaluate(results, group.Items, group.MediaType, minConfidence);

            foreach (var (reason, count) in gate.RejectedByReason)
            {
                Telemetry.Rejected.Add(count, new KeyValuePair<string, object?>("reason", reason));
            }

            return new GroupEvaluated(group.RuleSetId, gate.Accepted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new GroupFailed(group.RuleSetId, ex.Message);
        }
    }

    private async Task<IEnumerable<CrawlStep>> PublishBatch(ImmutableList<CrawlStep> batch, CancellationToken ct)
    {
        var entries = batch.OfType<GroupEvaluated>().SelectMany(g => g.Accepted).ToArray();
        if (entries.Length == 0)
        {
            return batch;
        }

        try
        {
            return batch.Add(new EntriesPublished(await stages.Publish(entries, ct)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return batch.Add(new PublishFailed(ex.Message));
        }
    }

    private async Task<MediathekItem[]> QueryItems(int offset, int size, CancellationToken ct) =>
        (await Query(offset, size, ct)).Items;

    private async Task<QueryMediathekCompleted> Query(int offset, int size, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            switch (await stages.QueryPage(offset, size, ct))
            {
                case QueryMediathekCompleted completed:
                    return completed;
                case QueryMediathekQueueFull when attempt < MaxQueueRetries:
                    await Task.Delay(_queueRetryDelay * (attempt + 1), timeProvider, ct);
                    break;
                case QueryMediathekQueueFull:
                    throw new InvalidOperationException("MediathekViewWeb queue full");
                case QueryMediathekError error:
                    throw new InvalidOperationException($"MediathekViewWeb query failed: {error.Cause.Message}", error.Cause);
                case var other:
                    throw new InvalidOperationException($"Unexpected response {other.GetType().Name}");
            }
        }
    }

    private async Task<FeedRunOutcome> Complete(Task<FeedRunStats> stats, long started) =>
        (await stats).ToOutcome(timeProvider.GetElapsedTime(started));

    private static IEnumerable<int> PageOffsets(FeedOptions options) =>
        Enumerable.Range(0, Math.Max(1, options.MaxPages)).Select(page => page * Math.Max(1, options.PageSize));
}

internal sealed record CrawlPlan(CrawlProbed Probed, CrawlWindow Window, RuleSetLookup Lookup);
