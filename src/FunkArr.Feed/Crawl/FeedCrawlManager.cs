using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core.Configuration;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;
using Microsoft.Extensions.Options;
using Terp.Diagnostics;

namespace FunkArr.Feed.Crawl;

public sealed class FeedCrawlManager : ReceivePersistentActor
{
    private const int SnapshotInterval = 20;

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IOptionsMonitor<FeedOptions> _feedOptions;
    private readonly TimeProvider _timeProvider;
    private FeedCrawlManagerState _state = new();
    private IActorRef? _worker;
    private bool _running;
    private bool _resultReceived;

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
        Recover<RecoveryCompleted>(_ => { });

        Command<StartFeedCrawl>(_ => StartCrawl());
        Command<QueryFeedCrawlStatus>(_ =>
            Sender.Tell(_state.ToStatus(_feedOptions.CurrentValue.Enabled, _running)));
        Command<CrawlResult>(HandleResult);
        Command<Terminated>(HandleTerminated);
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

    private void StartCrawl()
    {
        if (_running)
        {
            return;
        }

        var feed = _feedOptions.CurrentValue;
        if (!feed.Enabled)
        {
            return;
        }

        _running = true;
        _resultReceived = false;
        _log.Debug("Feed crawl started at cursor {Cursor}", _state.Cursor);

        var input = new CrawlInput(
            _state.Cursor, Math.Max(1, feed.PageSize), Math.Max(1, feed.MaxPages),
            Math.Max(1, feed.PagePrefetch), feed.MaxConcurrency, feed.MinConfidence,
            feed.InitialLookback, feed.MaxCatchUp, _timeProvider.GetTimestamp());

        _worker = Context.ActorOf(
            Props.Create(() => new FeedCrawlWorker(input, _timeProvider)),
            $"crawl-{Guid.NewGuid():N}");
        Context.Watch(_worker);
    }

    private void HandleResult(CrawlResult result)
    {
        _resultReceived = true;
        _running = false;
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

    }

    private void HandleTerminated(Terminated msg)
    {
        if (_worker is null || !msg.ActorRef.Equals(_worker))
        {
            return;
        }

        _worker = null;

        if (_resultReceived)
        {
            return;
        }

        _log.Warning("Feed crawl worker terminated unexpectedly without reporting a result");
        _running = false;
    }

    private static void RecordTelemetry(CrawlResult result)
    {
        var outcomeTag = new KeyValuePair<string, object?>("status", result.Status.ToString().ToLowerInvariant());
        Telemetry.Crawls.Up(outcomeTag);
        Telemetry.CrawlDuration.Record(result.Duration.TotalSeconds, outcomeTag);

        if (result.Status == CrawlOutcome.Unchanged)
        {
            Telemetry.ProbesUnchanged.Up();
        }

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

    private void MaybeSnapshot()
    {
        if (LastSequenceNr % SnapshotInterval == 0)
        {
            SaveSnapshot(_state.GetPersistenceState());
        }
    }
}
