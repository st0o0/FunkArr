using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core.Configuration;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;
using Microsoft.Extensions.Options;
using Terp.Diagnostics;

namespace FunkArr.Feed.Crawl;

public sealed class FeedCrawlManager : ReceivePersistentActor, IWithTimers
{
    private const int SnapshotInterval = 20;
    private const string TimerKey = "crawl";

    private static readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(30);

    private sealed record Tick;

    private sealed record OptionsChanged;

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IOptionsMonitor<FeedOptions> _feedOptions;
    private readonly TimeProvider _timeProvider;
    private FeedCrawlManagerState _state = new();
    private IActorRef _harvester = ActorRefs.Nobody;
    private bool _harvesting;

    public ITimerScheduler Timers { get; set; } = null!;

    public override string PersistenceId => "feed-crawl";

    public FeedCrawlManager(
        IOptionsMonitor<FeedOptions> feedOptions,
        TimeProvider timeProvider)
    {
        _feedOptions = feedOptions;
        _timeProvider = timeProvider;

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

            _harvester = Context.ActorOf(
                Props.Create(() => new FeedHarvestActor(timeProvider)), "harvester");

            var self = Self;
            _feedOptions.OnChange((_, _) => self.Tell(new OptionsChanged()));

            ScheduleNext(_startupDelay);
        });

        Command<Tick>(_ =>
        {
            if (_state.Enabled && !_harvesting)
            {
                StartHarvest();
            }

            ScheduleNext(_state.Interval);
        });
        Command<StartFeedCrawl>(_ =>
        {
            if (!_harvesting)
            {
                StartHarvest();
            }
        });
        Command<OptionsChanged>(_ =>
        {
            var previousInterval = _state.Interval;
            _state.Apply(_feedOptions.CurrentValue);

            if (_state.Interval != previousInterval)
            {
                _log.Info("Feed crawl interval changed from {Old} to {New}",
                    previousInterval, _state.Interval);
                ScheduleNext(_state.Interval);
            }
        });
        Command<QueryFeedCrawlStatus>(_ =>
            Sender.Tell(_state.ToStatus(_state.Enabled, _harvesting)));

        Command<HarvestCompleted>(msg => CompleteHarvest(msg.Summary));
        Command<HarvestUnchanged>(_ =>
        {
            _state.WithUnchangedRun(_timeProvider.GetUtcNow());
            RecordUnchangedTelemetry();
            _harvesting = false;
        });
        Command<HarvestFailed>(msg =>
        {
            var summary = new IngestSummary(CrawlOutcome.Failed, 0,
                0, 0, 0, false, TimeSpan.Zero, msg.Reason);
            CompleteHarvest(summary);
        });

        Command<SaveSnapshotSuccess>(e =>
        {
            DeleteMessages(e.Metadata.SequenceNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(e.Metadata.SequenceNr - 1));
        });
        Command<SaveSnapshotFailure>(f =>
            _log.Warning(f.Cause, "Feed crawl snapshot save failed at sequence {SequenceNr}",
                f.Metadata.SequenceNr));
        Command<DeleteMessagesSuccess>(_ => { });
        Command<DeleteMessagesFailure>(e =>
            _log.Warning(e.Cause, "Feed crawl journal cleanup failed up to sequence {SequenceNr}",
                e.ToSequenceNr));
        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteSnapshotsFailure>(e =>
            _log.Warning(e.Cause, "Feed crawl snapshot cleanup failed"));
    }

    private void ScheduleNext(TimeSpan delay) =>
        Timers.StartSingleTimer(TimerKey, new Tick(), delay);

    private void StartHarvest()
    {
        if (!_state.Enabled)
        {
            return;
        }

        _harvesting = true;
        _log.Debug("Feed crawl started at cursor {Cursor}", _state.Cursor);
        _harvester.Tell(new StartHarvest(_state.Cursor, _feedOptions.CurrentValue));
    }

    private void CompleteHarvest(IngestSummary summary)
    {
        RecordTelemetry(summary);
        var now = _timeProvider.GetUtcNow();

        switch (summary.Status)
        {
            case CrawlOutcome.Completed:
            case CrawlOutcome.CompletedWithGap:
                Persist(new FeedCursorAdvanced(summary.ListTimestamp, now,
                    summary.ItemsFound, summary.Accepted, summary.Unmatched,
                    summary.Truncated), e =>
                {
                    _state.Apply(e);
                    _log.Info(
                        "Feed crawl completed: cursor {Cursor}, {NewItems} new, {Published} published, {Unmatched} unmatched, gap {Gap}",
                        e.Cursor, e.NewItems, e.Published, e.Unmatched, e.Gap);
                    MaybeSnapshot();
                });
                break;

            case CrawlOutcome.Failed:
                var failures = _state.ConsecutiveFailures + 1;
                if (failures >= FeedCrawlManagerState.MaxConsecutiveFailures
                    && summary.ListTimestamp > _state.Cursor)
                {
                    Persist(new FeedCursorAdvanced(summary.ListTimestamp, now,
                        summary.ItemsFound, summary.Accepted, summary.Unmatched,
                        Gap: true), e =>
                    {
                        _state.Apply(e);
                        _log.Warning(
                            "Feed crawl failed ({Failures} in a row), advancing cursor with gap: {Reason}",
                            failures, summary.Error);
                        MaybeSnapshot();
                    });
                }
                else
                {
                    Persist(new FeedRunFailed(now, summary.Error ?? "unknown error", failures), e =>
                    {
                        _state.Apply(e);
                        _log.Warning("Feed crawl failed ({Failures} in a row): {Reason}",
                            e.ConsecutiveFailures, e.Reason);
                        MaybeSnapshot();
                    });
                }

                break;
        }

        _harvesting = false;
    }

    private static void RecordTelemetry(IngestSummary summary)
    {
        var outcomeTag = new KeyValuePair<string, object?>("status",
            summary.Status.ToString().ToLowerInvariant());
        Telemetry.Crawls.Up(outcomeTag);
        Telemetry.CrawlDuration.Record(summary.Duration.TotalSeconds, outcomeTag);

        if (summary.ItemsFound > 0)
        {
            Telemetry.ItemsSeen.Add(summary.ItemsFound);
        }

        if (summary.Unmatched > 0)
        {
            Telemetry.Unmatched.Add(summary.Unmatched);
        }

        if (summary.Accepted > 0)
        {
            Telemetry.Published.Add(summary.Accepted);
        }

        if (summary.Truncated)
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
