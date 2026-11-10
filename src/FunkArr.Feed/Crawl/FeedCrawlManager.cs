using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Routing;
using Akka.Streams;
using Akka.Streams.Dsl;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Persistence.Events.Feed;
using Microsoft.Extensions.Options;
using Terp.Akka;
using Terp.Diagnostics;

namespace FunkArr.Feed.Crawl;

public sealed class FeedCrawlManager : ReceivePersistentActor, IWithTimers
{
    private const int SnapshotInterval = 20;
    private const string TimerKey = "crawl";

    private static readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _publishTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _pageTimeout = TimeSpan.FromSeconds(120);

    private sealed record Tick;

    private sealed record OptionsChanged;

    private sealed record ProbeResult(long ListTimestamp);

    private sealed record ProbeFailed(string Reason);

    private sealed record RuleSetsLoaded(IReadOnlyList<RegisteredRuleSetEntry> Entries);

    private sealed record RuleSetsFailed(string Reason);

    private sealed record CrawlPagesCompleted(IImmutableList<ProcessPageResponse> Results);

    private sealed record CrawlPagesFailed(string Reason);

    private sealed record PublishCompleted(int Published);

    private sealed record PublishFailed(string Reason);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _mediathek;
    private readonly IActorRef _ruleSetResolver;
    private readonly IActorRef _store;
    private readonly IOptionsMonitor<FeedOptions> _feedOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IMaterializer _materializer = Context.Materializer();
    private FeedCrawlManagerState _state = new();
    private FeedCrawlRunState? _runState;
    private bool _running;
    private IActorRef _router = ActorRefs.Nobody;
    private int _poolSize;
    private bool _pendingResize;

    public ITimerScheduler Timers { get; set; } = null!;

    public override string PersistenceId => "feed-crawl";

    public FeedCrawlManager(
        IOptionsMonitor<FeedOptions> feedOptions,
        TimeProvider timeProvider)
    {
        _feedOptions = feedOptions;
        _timeProvider = timeProvider;
        _mediathek = Context.GetActor<IMediathekManager>();
        _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
        _store = Context.GetActor<IFeedStoreManager>();

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is PersistedFeedCrawlState persisted)
            {
                _state = FeedCrawlManagerState.FromPersistence(persisted);
            }
        });
        Recover<FeedCursorAdvanced>(evt => _state.Apply(evt));
        Recover<FeedRunFailed>(evt => _state.Apply(evt));
        Recover<RecoveryCompleted>(_ =>
        {
            _state.Apply(_feedOptions.CurrentValue);

            _poolSize = _feedOptions.CurrentValue.MaxConcurrency;
            _router = Context.ActorOf(
                Props.Create<FeedCrawlPageActor>()
                    .WithRouter(new SmallestMailboxPool(_poolSize)),
                "page-pool");

            var self = Self;
            _feedOptions.OnChange((_, _) => self.Tell(new OptionsChanged()));

            ScheduleNext(_startupDelay);
        });

        Idle();
    }

    private void RegisterSnapshotHandlers()
    {
        Command<SaveSnapshotSuccess>(e =>
        {
            DeleteMessages(e.Metadata.SequenceNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(e.Metadata.SequenceNr - 1));
        });
        Command<SaveSnapshotFailure>(f =>
            _log.Warning(f.Cause, "Feed crawl snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));
        Command<DeleteMessagesSuccess>(_ => { });
        Command<DeleteMessagesFailure>(e =>
            _log.Warning(e.Cause, "Feed crawl journal cleanup failed up to sequence {SequenceNr}", e.ToSequenceNr));
        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteSnapshotsFailure>(e => _log.Warning(e.Cause, "Feed crawl snapshot cleanup failed"));
    }

    private void RegisterStatusHandler()
    {
        Command<QueryFeedCrawlStatus>(_ =>
            Sender.Tell(_state.ToStatus(_state.Enabled, _running)));
    }

    private void Idle()
    {
        Command<Tick>(_ =>
        {
            if (_state.Enabled)
            {
                StartProbe();
            }

            ScheduleNext(_state.Interval);
        });
        Command<StartFeedCrawl>(_ => StartProbe());
        Command<OptionsChanged>(_ =>
        {
            var previousInterval = _state.Interval;
            _state.Apply(_feedOptions.CurrentValue);

            if (_state.Interval != previousInterval)
            {
                _log.Info("Feed crawl interval changed from {Old} to {New}", previousInterval, _state.Interval);
                ScheduleNext(_state.Interval);
            }

            AdjustPoolIfNeeded();
        });
        RegisterStatusHandler();
        RegisterSnapshotHandlers();
    }

    private void ScheduleNext(TimeSpan delay) => Timers.StartSingleTimer(TimerKey, new Tick(), delay);

    private void AdjustPoolIfNeeded()
    {
        var newSize = _feedOptions.CurrentValue.MaxConcurrency;
        if (newSize != _poolSize)
        {
            _router.Tell(new AdjustPoolSize(newSize - _poolSize));
            _poolSize = newSize;
            _pendingResize = false;
        }
    }

    private void StartProbe()
    {
        if (!_state.Enabled)
        {
            return;
        }

        _running = true;
        _log.Debug("Feed crawl started at cursor {Cursor}", _state.Cursor);

        _mediathek.Ask<QueryMediathekResponse>(
                new QueryMediathek([], "filmlisteTimestamp", "desc", Future: true, 0, 1, 300, null),
                _mediathekTimeout)
            .PipeTo(Self,
                success: response => response switch
                {
                    QueryMediathekCompleted c => new ProbeResult(c.ListTimestamp),
                    QueryMediathekError e => new ProbeFailed($"MediathekViewWeb query failed: {e.Cause.Message}"),
                    _ => new ProbeFailed("MediathekViewWeb queue full")
                },
                failure: ex => new ProbeFailed(ex.Message));

        Become(Probing);
    }

    private void Probing()
    {
        Command<ProbeResult>(probe =>
        {
            if (probe.ListTimestamp <= 0 || probe.ListTimestamp <= _state.Cursor)
            {
                _state.WithUnchangedRun(_timeProvider.GetUtcNow());
                RecordUnchangedTelemetry();
                ReturnToIdle();
                return;
            }

            var feed = _feedOptions.CurrentValue;
            var (lower, upper, truncated) = ComputeWindow(
                _state.Cursor, probe.ListTimestamp, feed.MaxCatchUp, feed.InitialLookback);

            _runState = FeedCrawlRunState.Init(
                probe.ListTimestamp, lower, upper, truncated,
                Math.Max(1, feed.PageSize), Math.Max(1, feed.MaxPages),
                feed.MinConfidence, _timeProvider.GetTimestamp());

            _ruleSetResolver.Ask<RegisteredRuleSetsResult>(
                    new QueryRegisteredRuleSets(), _ruleSetTimeout)
                .PipeTo(Self,
                    success: result => new RuleSetsLoaded(result.Entries),
                    failure: ex => new RuleSetsFailed(ex.Message));
        });

        Command<RuleSetsLoaded>(msg =>
        {
            _runState!.Apply(msg.Entries);

            _log.Debug(
                "Feed crawl probed: listTimestamp={ListTimestamp}, window=[{Lower},{Upper}], truncated={Truncated}",
                _runState.ListTimestamp, _runState.LowerBound, _runState.UpperBound,
                _runState.Truncated);

            StartCrawling();
        });

        Command<ProbeFailed>(msg =>
        {
            _log.Warning("Feed probe failed: {Reason}", msg.Reason);
            ReturnToIdle();
        });

        Command<RuleSetsFailed>(msg =>
        {
            _log.Warning("Feed ruleset loading failed: {Reason}", msg.Reason);
            _runState = null;
            ReturnToIdle();
        });

        Command<Tick>(_ => ScheduleNext(_state.Interval));
        Command<OptionsChanged>(_ =>
        {
            _state.Apply(_feedOptions.CurrentValue);
            _pendingResize = true;
        });
        RegisterStatusHandler();
        RegisterSnapshotHandlers();
    }

    private static (long lower, long upper, bool truncated) ComputeWindow(
        long cursor, long listTimestamp, TimeSpan maxCatchUp, TimeSpan initialLookback)
    {
        var catchUp = (long)maxCatchUp.TotalSeconds;

        if (cursor <= 0)
        {
            return (Math.Max(0, listTimestamp - (long)initialLookback.TotalSeconds), listTimestamp, false);
        }

        if (listTimestamp - catchUp is var earliest && cursor < earliest)
        {
            return (earliest, listTimestamp, true);
        }

        return (cursor, listTimestamp, false);
    }

    private void StartCrawling()
    {
        var run = _runState!;
        var pageSize = run.PageSize;
        var topicTargets = run.TopicTargets!;

        var messages = Enumerable.Range(0, run.MaxPages)
            .Select(i => new ProcessPage(
                i * pageSize, pageSize,
                run.LowerBound, run.UpperBound,
                topicTargets, run.MinConfidence))
            .ToArray();

        Source.From(messages)
            .SelectAsync(_poolSize, msg => _router.Ask<ProcessPageResponse>(msg, _pageTimeout))
            .TakeWhile(r => r is not ProcessPageCompleted { ReachedEnd: true }, inclusive: true)
            .RunWith(Sink.Seq<ProcessPageResponse>(), _materializer)
            .PipeTo(Self,
                success: results => new CrawlPagesCompleted(results),
                failure: ex => new CrawlPagesFailed(ex.Message));

        Become(Crawling);
    }

    private void Crawling()
    {
        Command<CrawlPagesCompleted>(msg =>
        {
            var run = _runState!;
            run.Apply(msg.Results);

            if (run.AcceptedEntries.Count == 0)
            {
                CompleteCrawl(run.GetCrawlResult(Elapsed()));
                return;
            }

            _store.Ask<PublishFeedEntriesResponse>(
                    new PublishFeedEntries([.. run.AcceptedEntries]), _publishTimeout)
                .PipeTo(Self,
                    success: response => response switch
                    {
                        PublishFeedEntriesCompleted c => new PublishCompleted(c.Published),
                        PublishFeedEntriesFailed f => new PublishFailed(f.Cause.Message),
                        _ => new PublishFailed("unexpected publish response")
                    },
                    failure: ex => new PublishFailed(ex.Message));
        });

        Command<PublishCompleted>(_ =>
            CompleteCrawl(_runState!.GetCrawlResult(Elapsed())));

        Command<PublishFailed>(msg =>
        {
            var run = _runState!;
            CompleteCrawl(new CrawlResult(CrawlOutcome.Failed, run.ListTimestamp,
                run.TotalNewItems, 0, run.TotalUnmatched,
                run.Truncated, Elapsed(), msg.Reason));
        });

        Command<CrawlPagesFailed>(msg =>
        {
            var run = _runState!;
            CompleteCrawl(new CrawlResult(CrawlOutcome.Failed, run.ListTimestamp,
                0, 0, 0, run.Truncated, Elapsed(), msg.Reason));
        });

        Command<Tick>(_ => ScheduleNext(_state.Interval));
        Command<OptionsChanged>(_ =>
        {
            _state.Apply(_feedOptions.CurrentValue);
            _pendingResize = true;
        });
        RegisterStatusHandler();
        RegisterSnapshotHandlers();
    }

    private TimeSpan Elapsed() => _timeProvider.GetElapsedTime(_runState!.StartedAt);

    private void CompleteCrawl(CrawlResult result)
    {
        RecordTelemetry(result);
        var now = _timeProvider.GetUtcNow();

        switch (result.Status)
        {
            case CrawlOutcome.Completed:
            case CrawlOutcome.CompletedWithGap:
                Persist(new FeedCursorAdvanced(result.ListTimestamp, now, result.NewItems, result.Published,
                    result.Unmatched, result.Gap), e =>
                {
                    _state.Apply(e);
                    _log.Info(
                        "Feed crawl completed: cursor {Cursor}, {NewItems} new entries, {Published} published, {Unmatched} without ruleset, gap {Gap}",
                        e.Cursor, e.NewItems, e.Published, e.Unmatched, e.Gap);
                    MaybeSnapshot();
                });
                break;

            case CrawlOutcome.Failed:
                var failures = _state.ConsecutiveFailures + 1;
                if (failures >= FeedCrawlManagerState.MaxConsecutiveFailures && result.ListTimestamp > _state.Cursor)
                {
                    Persist(new FeedCursorAdvanced(result.ListTimestamp, now, result.NewItems, result.Published,
                        result.Unmatched, Gap: true), e =>
                    {
                        _state.Apply(e);
                        _log.Warning(
                            "Feed crawl failed ({Failures} in a row), advancing cursor with gap: {Reason}",
                            failures, result.Error);
                        MaybeSnapshot();
                    });
                }
                else
                {
                    Persist(new FeedRunFailed(now, result.Error ?? "unknown error", failures), e =>
                    {
                        _state.Apply(e);
                        _log.Warning("Feed crawl failed ({Failures} in a row): {Reason}", e.ConsecutiveFailures,
                            e.Reason);
                        MaybeSnapshot();
                    });
                }

                break;

            default:
                _state.WithUnchangedRun(now);
                break;
        }

        _runState = null;
        ReturnToIdle();
    }

    private void ReturnToIdle()
    {
        _running = false;

        if (_pendingResize)
        {
            AdjustPoolIfNeeded();
        }

        Become(Idle);
    }

    private static void RecordTelemetry(CrawlResult result)
    {
        var outcomeTag = new KeyValuePair<string, object?>("status", result.Status.ToString().ToLowerInvariant());
        Telemetry.Crawls.Up(outcomeTag);
        Telemetry.CrawlDuration.Record(result.Duration.TotalSeconds, outcomeTag);

        if (result.NewItems > 0)
        {
            Telemetry.ItemsSeen.Add(result.NewItems);
        }

        if (result.Unmatched > 0)
        {
            Telemetry.Unmatched.Add(result.Unmatched);
        }

        if (result.Published > 0)
        {
            Telemetry.Published.Add(result.Published);
        }

        if (result.Gap)
        {
            Telemetry.Gaps.Up();
        }
    }

    private static void RecordUnchangedTelemetry()
    {
        Telemetry.Crawls.Up(new KeyValuePair<string, object?>("status", "unchanged"));
        Telemetry.ProbesUnchanged.Up();
    }

    private void MaybeSnapshot()
    {
        if (LastSequenceNr % SnapshotInterval == 0)
        {
            SaveSnapshot(_state.GetPersistenceState());
        }
    }
}
