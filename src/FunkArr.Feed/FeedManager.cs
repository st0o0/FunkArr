using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Feed;

public sealed class FeedManager : ReceivePersistentActor, IWithTimers
{
    private const int _snapshotInterval = 50;
    private const string _trimTimerKey = "trim";
    private static readonly TimeSpan _trimInterval = TimeSpan.FromHours(1);

    private sealed record TrimFeed;

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _feedCache = Context.GetActor<IFeedCache>();
    private readonly IOptionsMonitor<FeedOptions> _options;
    private readonly TimeProvider _timeProvider;
    private FeedManagerState _state = FeedManagerState.Empty;

    public ITimerScheduler Timers { get; set; } = null!;

    public override string PersistenceId => "feed";

    public FeedManager(IOptionsMonitor<FeedOptions> options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is PersistedFeedState persisted)
            {
                _state = FeedManagerState.FromPersistence(persisted);
            }
        });
        Recover<FeedEntriesAdded>(evt => _state = _state.Apply(evt));
        Recover<FeedEntriesExpired>(evt => _state = _state.Apply(evt));
        Recover<RecoveryCompleted>(_ =>
        {
            Telemetry.SetEntries(_state.Entries.Count);
            _feedCache.Tell(new FeedChanged());
            Timers.StartPeriodicTimer(_trimTimerKey, new TrimFeed(), _trimInterval);
        });

        Command<AddFeedEntries>(HandleAdd);
        Command<QueryFeed>(query => Sender.Tell(_state.Query(query)));
        Command<TrimFeed>(_ => Trim());
        Command<SaveSnapshotSuccess>(e =>
        {
            DeleteMessages(e.Metadata.SequenceNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(e.Metadata.SequenceNr - 1));
        });
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "Feed snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));
        Command<DeleteMessagesSuccess>(_ => { });
        Command<DeleteMessagesFailure>(e => _log.Warning(e.Cause, "Feed journal cleanup failed up to sequence {SequenceNr}", e.ToSequenceNr));
        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteSnapshotsFailure>(e => _log.Warning(e.Cause, "Feed snapshot cleanup failed"));
    }

    private void HandleAdd(AddFeedEntries cmd)
    {
        var fresh = _state.NewEntries(cmd.Entries);
        if (fresh.Length == 0)
        {
            Sender.Tell(new AddFeedEntriesCompleted(0));
            return;
        }

        var evt = new FeedEntriesAdded([.. fresh.Select(e => e.ToPersistence())]);
        Persist(evt, e =>
        {
            _state = _state.Apply(e);
            Telemetry.SetEntries(_state.Entries.Count);
            _feedCache.Tell(new FeedChanged());
            MaybeSnapshot();
            Sender.Tell(new AddFeedEntriesCompleted(fresh.Length));
        });

        Trim();
    }

    private void Trim()
    {
        var expired = _state.ExpiredIds(_timeProvider.GetUtcNow(), _options.CurrentValue.Retention);
        if (expired.Length == 0)
        {
            return;
        }

        Persist(new FeedEntriesExpired(expired), e =>
        {
            _state = _state.Apply(e);
            Telemetry.SetEntries(_state.Entries.Count);
            _feedCache.Tell(new FeedChanged());
            _log.Info("Removed {Count} feed entries older than the retention period", e.StableIds.Length);
            MaybeSnapshot();
        });
    }

    private void MaybeSnapshot()
    {
        if (LastSequenceNr % _snapshotInterval == 0)
        {
            SaveSnapshot(_state.GetPersistenceState());
        }
    }
}
