using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Streams;
using FunkArr.Core;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Feed;

public sealed class FeedCrawlManager : ReceivePersistentActor, IWithTimers
{
    private const int _snapshotInterval = 20;
    private const string _tickTimerKey = "tick";
    private static readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(30);

    private sealed record Tick;
    private sealed record RunFinished(FeedRunOutcome Outcome);
    private sealed record RunCrashed(Exception Cause);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IOptionsMonitor<FeedOptions> _feedOptions;
    private readonly TimeProvider _timeProvider;
    private readonly FeedCrawlPipeline _pipeline;
    private readonly IMaterializer _materializer = Context.Materializer();
    private FeedCrawlManagerState _state = FeedCrawlManagerState.Empty;
    private RunningCrawl? _run;

    public ITimerScheduler Timers { get; set; } = null!;

    public override string PersistenceId => "feed-crawl";

    public FeedCrawlManager(
        IOptionsMonitor<FeedOptions> feedOptions,
        TimeProvider timeProvider)
    {
        _feedOptions = feedOptions;
        _timeProvider = timeProvider;
        _pipeline = new FeedCrawlPipeline(
            FeedCrawlStages.FromActors(
                Context.GetActor<IMediathekManager>(),
                Context.GetActor<IRuleSetResolver>(),
                Context.GetActor<ITvEvaluationRegion>(),
                Context.GetActor<IMovieEvaluationRegion>(),
                Context.GetActor<IFeedStoreManager>()),
            timeProvider);

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
        Command<RunFinished>(msg => HandleOutcome(msg.Outcome));
        Command<RunCrashed>(msg =>
        {
            _log.Error(msg.Cause, "Feed crawl crashed");
            HandleOutcome(new FeedRunOutcome(FeedRunStatus.Failed, 0, 0, 0, 0, false, TimeSpan.Zero, msg.Cause.Message));
        });
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
        _log.Debug("Feed crawl started at cursor {Cursor}", _state.Cursor);

        _run = _pipeline.Run(_state.Cursor, feed, _materializer);
        _run.Completion.PipeTo(Self, success: outcome => new RunFinished(outcome), failure: ex => new RunCrashed(ex));
    }

    protected override void PostStop()
    {
        _run?.Cancel();
        base.PostStop();
    }

    private void HandleOutcome(FeedRunOutcome outcome)
    {
        _run?.Cancellation.Dispose();
        _run = null;
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
