using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Streams;
using Akka.Streams.Dsl;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Persistence.Events.Feed;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Feed;

public sealed class FeedCrawlManager : ReceivePersistentActor, IWithTimers
{
    private const int _snapshotInterval = 20;
    private const int _pagePrefetch = 3;
    private const int _maxSubstreams = 500;
    private const int _groupSize = 50;
    private const int _publishBatchSize = 200;
    private const string _tickTimerKey = "tick";

    private static readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _publishTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _groupWindow = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _publishWindow = TimeSpan.FromSeconds(2);

    private sealed record Tick;
    private sealed record Probed(Guid RunId, QueryMediathekResponse Response);
    private sealed record RuleSetsLoaded(Guid RunId, RegisteredRuleSetEntry[] Entries, long ListTimestamp);
    private sealed record RuleSetsFailed(Guid RunId, Exception Cause);
    private sealed record CrawlOffered(Guid RunId, IQueueOfferResult? Result, Exception? Error);
    private sealed record CrawlStreamCompleted;
    private sealed record CrawlStreamFailed(Exception Cause);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IOptionsMonitor<FeedOptions> _feedOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IActorRef _self;
    private readonly IActorRef _mediathek = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _tvEvaluation = Context.GetActor<ITvEvaluationRegion>();
    private readonly IActorRef _movieEvaluation = Context.GetActor<IMovieEvaluationRegion>();
    private readonly IActorRef _store = Context.GetActor<IFeedStoreManager>();
    private readonly ISourceQueueWithComplete<FeedCrawlPlan> _crawls;
    private FeedCrawlManagerState _state = FeedCrawlManagerState.Empty;
    private FeedCrawlRun? _run;

    public ITimerScheduler Timers { get; set; } = null!;

    public override string PersistenceId => "feed-crawl";

    public FeedCrawlManager(
        IOptionsMonitor<FeedOptions> feedOptions,
        TimeProvider timeProvider)
    {
        _feedOptions = feedOptions;
        _timeProvider = timeProvider;
        _self = Self;

        _crawls = Source.Queue<FeedCrawlPlan>(1, OverflowStrategy.DropNew)
            .Via(Flow.Create<FeedCrawlPlan>().ConcatMany(Crawl))
            .To(Sink.ActorRef<FeedCrawlEvent>(_self, new CrawlStreamCompleted(), ex => new CrawlStreamFailed(ex)))
            .Run(Context.Materializer());

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is PersistedFeedCrawlState persisted)
            {
                _state = FeedCrawlManagerState.FromPersistence(persisted);
            }
        });
        Recover<FeedCursorAdvanced>(evt => _state = _state.Apply(evt));
        Recover<FeedRunFailed>(evt => _state = _state.Apply(evt));
        Recover<RecoveryCompleted>(_ => ScheduleNext(_startupDelay));

        Command<Tick>(_ => StartRun());
        Command<StartFeedCrawl>(_ => StartRun());
        Command<QueryFeedCrawlStatus>(_ =>
            Sender.Tell(_state.ToStatus(_feedOptions.CurrentValue.Enabled, _run is not null)));
        Command<Probed>(msg => WhenCurrent(msg.RunId, () => HandleProbe(msg.Response)));
        Command<RuleSetsLoaded>(msg => WhenCurrent(msg.RunId, () => Offer(msg)));
        Command<RuleSetsFailed>(msg => WhenCurrent(msg.RunId, () => Finish(_run!.Failed($"ruleset lookup failed: {msg.Cause.Message}", Elapsed()))));
        Command<CrawlOffered>(msg => WhenCurrent(msg.RunId, () =>
        {
            if (msg.Result is not QueueOfferResult.Enqueued)
            {
                Finish(_run!.Failed(msg.Error?.Message ?? "feed crawl stream is busy", Elapsed()));
            }
        }));
        Command<FeedCrawlFinished>(msg => WhenCurrent(msg.RunId, () => Finish(_run!.ToOutcome(Elapsed()))));
        Command<FeedCrawlFailed>(msg => WhenCurrent(msg.RunId, () => Finish(_run!.Apply(msg).ToOutcome(Elapsed()))));
        Command<FeedCrawlEvent>(msg => WhenCurrent(msg.RunId, () => _run = _run!.Apply(msg)));
        Command<CrawlStreamCompleted>(_ => throw new InvalidOperationException("Feed crawl stream completed unexpectedly"));
        Command<CrawlStreamFailed>(msg => throw new InvalidOperationException("Feed crawl stream failed", msg.Cause));
        Command<SaveSnapshotSuccess>(e => DeleteMessages(e.Metadata.SequenceNr));
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "Feed crawl snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));
        Command<DeleteMessagesSuccess>(_ => { });
        Command<DeleteMessagesFailure>(e => _log.Warning(e.Cause, "Feed crawl journal cleanup failed up to sequence {SequenceNr}", e.ToSequenceNr));
    }

    private void StartRun()
    {
        if (_run is not null)
        {
            return;
        }

        var feed = _feedOptions.CurrentValue;
        if (!feed.Enabled)
        {
            ScheduleNext(feed.Interval);
            return;
        }

        Timers.Cancel(_tickTimerKey);
        var runId = Guid.NewGuid();
        _run = FeedCrawlRun.Start(runId, _timeProvider.GetTimestamp());
        _log.Debug("Feed crawl {RunId} started at cursor {Cursor}", runId, _state.Cursor);

        _mediathek.Ask<QueryMediathekResponse>(FeedCrawlPlan.Query(0, 1), _mediathekTimeout)
            .PipeTo(Self,
                success: response => new Probed(runId, response),
                failure: ex => new Probed(runId, new QueryMediathekError(ex)));
    }

    private void HandleProbe(QueryMediathekResponse response)
    {
        if (response is not QueryMediathekCompleted { ListTimestamp: var listTimestamp })
        {
            Finish(_run!.Failed(Describe(response), Elapsed()));
            return;
        }

        if (!FeedCrawlPlanner.HasUpdate(_state.Cursor, listTimestamp))
        {
            Finish(_run!.Unchanged(listTimestamp, Elapsed()));
            return;
        }

        var runId = _run!.RunId;
        _ruleSetResolver.Ask<RegisteredRuleSetsResult>(new QueryRegisteredRuleSets(), _ruleSetTimeout)
            .PipeTo(Self,
                success: result => new RuleSetsLoaded(runId, result.Entries, listTimestamp),
                failure: ex => new RuleSetsFailed(runId, ex));
    }

    private void Offer(RuleSetsLoaded loaded)
    {
        var feed = _feedOptions.CurrentValue;
        var window = FeedCrawlPlanner.Window(_state.Cursor, loaded.ListTimestamp, feed.InitialLookback, feed.MaxCatchUp);
        var plan = FeedCrawlPlan.Create(loaded.RunId, loaded.ListTimestamp, window, RuleSetLookup.From(loaded.Entries),
            feed, _timeProvider.GetUtcNow());
        _run = _run!.Planned(plan);

        _crawls.OfferAsync(plan)
            .PipeTo(Self,
                success: result => new CrawlOffered(plan.RunId, result, null),
                failure: ex => new CrawlOffered(plan.RunId, null, ex));
    }

    private Source<FeedCrawlEvent, NotUsed> Crawl(FeedCrawlPlan plan)
    {
        var groups = (Source<IEnumerable<FeedMatch>, NotUsed>)Source.From(plan.PageOffsets)
            .SelectAsync(_pagePrefetch, async offset =>
                plan.Read(await _mediathek.Ask<QueryMediathekResponse>(plan.PageQuery(offset), _mediathekTimeout)))
            .WireTap(page => _self.Tell(new FeedPageCrawled(plan.RunId, page.ReachedEnd)))
            .TakeWhile(page => !page.ReachedEnd, inclusive: true)
            .SelectMany(page => page.Collected)
            .Where(plan.WithinLead)
            .StatefulSelectMany(FeedCrawlPlanner.DistinctByStableId)
            .WireTap(_ => _self.Tell(new FeedItemCollected(plan.RunId)))
            .SelectMany(plan.Match)
            .WireTap(_ => _self.Tell(new FeedItemMatched(plan.RunId)))
            .GroupBy(_maxSubstreams, match => match.Target.RuleSetId)
            .GroupedWithin(_groupSize, _groupWindow)
            .MergeSubstreams();

        return groups
            .SelectAsyncUnordered(_feedOptions.CurrentValue.MaxConcurrency, async matches =>
            {
                var group = FeedCrawlPlan.Group(matches);
                try
                {
                    return FeedEvaluation.Of(group, group.MediaType == MediaType.Movie
                        ? (await _movieEvaluation.Ask<EvaluateMoviesResponse>(FeedCrawlPlan.EvaluateMovies(group), _evaluationTimeout)).ItemsOrThrow()
                        : (await _tvEvaluation.Ask<EvaluateSeriesResponse>(FeedCrawlPlan.EvaluateSeries(group), _evaluationTimeout)).ItemsOrThrow());
                }
                catch (Exception ex)
                {
                    return FeedEvaluation.Failed(group, ex);
                }
            })
            .Select(evaluation => evaluation.Gate(plan.MinConfidence))
            .GroupedWithin(_publishBatchSize, _publishWindow)
            .SelectAsync(1, async batch =>
            {
                var gated = batch.ToArray();
                var entries = gated.SelectMany(g => g.Accepted).ToArray();
                try
                {
                    var published = entries.Length == 0
                        ? 0
                        : (await _store.Ask<PublishFeedEntriesResponse>(new PublishFeedEntries(entries), _publishTimeout)).PublishedOrThrow();
                    return (FeedCrawlEvent)new FeedBatchPublished(plan.RunId, gated.Length, gated.Count(g => g.Error is not null), published);
                }
                catch (Exception ex)
                {
                    return new FeedBatchPublishFailed(plan.RunId, gated.Length, ex.Message);
                }
            })
            .Concat(Source.Single<FeedCrawlEvent>(new FeedCrawlFinished(plan.RunId)))
            .Recover(ex => Akka.Util.Option<FeedCrawlEvent>.Create(new FeedCrawlFailed(plan.RunId, ex.Message)));
    }

    private void WhenCurrent(Guid runId, Action handle)
    {
        if (_run?.RunId == runId)
        {
            handle();
        }
    }

    private void Finish(FeedRunOutcome outcome)
    {
        _run = null;
        HandleOutcome(outcome);
    }

    private TimeSpan Elapsed() => _run is null ? TimeSpan.Zero : _timeProvider.GetElapsedTime(_run.StartedAt);

    private static string Describe(QueryMediathekResponse response) => response switch
    {
        QueryMediathekQueueFull => "MediathekViewWeb queue full",
        QueryMediathekError error => $"MediathekViewWeb query failed: {error.Cause.Message}",
        _ => "MediathekViewWeb query failed"
    };

    private void HandleOutcome(FeedRunOutcome outcome)
    {
        var now = _timeProvider.GetUtcNow();
        RecordTelemetry(outcome);

        switch (_state.Decide(outcome, now))
        {
            case FeedCursorAdvanced advanced:
                Persist(advanced, e =>
                {
                    _state = _state.Apply(e);
                    _log.Info(
                        "Feed crawl completed: cursor {Cursor}, {NewItems} new entries, {Published} published, {Unmatched} without ruleset, gap {Gap}",
                        e.Cursor, e.NewItems, e.Published, e.Unmatched, e.Gap);
                    MaybeSnapshot();
                });
                break;

            case FeedRunFailed failed:
                Persist(failed, e =>
                {
                    _state = _state.Apply(e);
                    _log.Warning("Feed crawl failed ({Failures} in a row): {Reason}", e.ConsecutiveFailures, e.Reason);
                    MaybeSnapshot();
                });
                break;

            default:
                _state = _state.WithUnchangedRun(now);
                break;
        }

        ScheduleNext(_feedOptions.CurrentValue.Interval);
    }

    private static void RecordTelemetry(FeedRunOutcome outcome)
    {
        var outcomeTag = new KeyValuePair<string, object?>("status", outcome.Status.ToString().ToLowerInvariant());
        Telemetry.Crawls.Add(1, outcomeTag);
        Telemetry.CrawlDuration.Record(outcome.Duration.TotalSeconds, outcomeTag);

        if (outcome.Status == FeedRunStatus.Unchanged)
        {
            Telemetry.ProbesUnchanged.Add(1);
        }

        if (outcome.NewItems > 0)
        {
            Telemetry.ItemsSeen.Add(outcome.NewItems);
        }

        if (outcome.Unmatched > 0)
        {
            Telemetry.Unmatched.Add(outcome.Unmatched);
        }

        if (outcome.Published > 0)
        {
            Telemetry.Published.Add(outcome.Published);
        }

        if (outcome.Gap)
        {
            Telemetry.Gaps.Add(1);
        }
    }

    private void ScheduleNext(TimeSpan delay) =>
        Timers.StartSingleTimer(_tickTimerKey, new Tick(), delay);

    private void MaybeSnapshot()
    {
        if (LastSequenceNr % _snapshotInterval == 0)
        {
            SaveSnapshot(_state.GetPersistenceState());
        }
    }
}
