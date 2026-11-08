using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Streams;
using Akka.Streams.Dsl;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;
using Microsoft.Extensions.Options;
using Terp.Akka;
using Terp.Diagnostics;

namespace FunkArr.Download.Manager;

public sealed class DownloadManager : ReceivePersistentActor
{
    private sealed record ConcurrencyChanged(int Limit);

    private const int _snapshotInterval = 25;
    private static readonly TimeSpan _fanOutTimeout = TimeSpan.FromSeconds(2);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _downloadRegion = Context.GetActor<IDownloadRegion>();
    private readonly IActorRef _historyManager = Context.GetActor<IDownloadHistoryManager>();
    private readonly IMaterializer _materializer = Context.Materializer();
    private DownloadManagerState _state = DownloadManagerState.Empty;

    public override string PersistenceId => "download-manager";

    public DownloadManager(IOptionsMonitor<DownloadOptions> options)
    {
        Self.Tell(new ConcurrencyChanged(options.CurrentValue.ConcurrentDownloads));
        Command<AddDownload>(HandleAdd);
        Command<SlotFree>(HandleSlotFree);
        Command<QueryQueue>(HandleQueryQueue);
        Command<DeleteDownload>(HandleDelete);
        Command<RetryDownload>(HandleRetry);
        Command<PauseDownloads>(HandlePause);
        Command<ResumeDownloads>(HandleResume);
        Command<ForceStartDownload>(HandleForceStart);
        Command<MoveDownload>(HandleMove);
        Command<SwapDownloads>(HandleSwap);
        Command<SetDownloadPriority>(HandleSetPriority);
        Command<ScheduleEnabled>(HandleScheduleEnabled);
        Command<ScheduleDisabled>(HandleScheduleDisabled);

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is PersistedDownloadManagerState persisted)
            {
                _state = DownloadManagerState.FromPersistence(persisted);
            }
        });
        Recover<DownloadEnqueued>(evt => _state.Apply(evt));
        Recover<DownloadDispatched>(evt => _state.Apply(evt));
        Recover<DownloadDequeued>(evt => _state.Apply(evt));
        Recover<DownloadMoved>(evt => _state.Apply(evt));
        Recover<DownloadSwapped>(evt => _state.Apply(evt));
        Recover<DownloadPriorityChanged>(evt => _state.Apply(evt));
        Recover<DownloadsPaused>(evt => _state.Apply(evt));
        Recover<DownloadsResumed>(evt => _state.Apply(evt));
        Recover<RecoveryCompleted>(_ =>
        {
            _state.ResetDispatched();
            UpdateGauges();
            DispatchNext();
        });

        Command<SaveSnapshotSuccess>(e =>
        {
            _log.Debug("Snapshot saved at sequence {SequenceNr}, cleaning journal", e.Metadata.SequenceNr);
            DeleteMessages(e.Metadata.SequenceNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(e.Metadata.SequenceNr - 1));
        });
        Command<SaveSnapshotFailure>(f =>
            _log.Warning(f.Cause, "Snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));
        Command<DeleteMessagesSuccess>(e => _log.Debug("Journal cleaned up to sequence {SequenceNr}", e.ToSequenceNr));
        Command<DeleteMessagesFailure>(e =>
            _log.Warning(e.Cause, "Journal cleanup failed up to sequence {SequenceNr}", e.ToSequenceNr));
        Command<DeleteSnapshotsSuccess>(e =>
            _log.Debug("Old snapshots cleaned up to sequence {SequenceNr}", e.Criteria.MaxSequenceNr));
        Command<DeleteSnapshotsFailure>(e => _log.Warning(e.Cause, "Snapshot cleanup failed"));

        var self = Self;
        options.OnChange(opts => self.Tell(new ConcurrencyChanged(opts.ConcurrentDownloads)));
        Command<ConcurrencyChanged>(msg =>
        {
            _state.MaxConcurrent = msg.Limit;
            DispatchNext();
        });
    }

    private void HandleAdd(AddDownload cmd)
    {
        var downloadId = Guid.NewGuid();

        _log.Info("Enqueuing download {DownloadId}: {Title} (channel: {Channel})", downloadId, cmd.Media.Title,
            cmd.Media.Channel);
        Persist(new DownloadEnqueued(downloadId, cmd.Priority.ToPersistence(), cmd.Media.Category.ToPersistence()), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();

            _downloadRegion.Tell(new InitDownload(downloadId, cmd.Media));

            Telemetry.Enqueued.Up();
            Sender.Tell(new DownloadAdded(downloadId));
            _log.Debug("Queue depth: {Queued} queued, {Dispatched} dispatched", _state.Queued.Count,
                _state.Dispatched.Count);
            UpdateGauges();
            DispatchNext();
        });
    }

    private void HandleSlotFree(SlotFree msg)
    {
        if (!_state.Dispatched.ContainsKey(msg.DownloadId))
        {
            return;
        }

        _log.Info("Download {DownloadId} slot freed", msg.DownloadId);
        Persist(new DownloadDequeued(msg.DownloadId), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
            _log.Debug("Queue depth: {Queued} queued, {Dispatched} dispatched", _state.Queued.Count,
                _state.Dispatched.Count);
            UpdateGauges();
            DispatchNext();
        });
    }

    private void HandleQueryQueue(QueryQueue query)
    {
        var (pageIds, totalItems) = _state.GetPage(query);

        if (pageIds.Length == 0)
        {
            Sender.Tell(new QueueResult([], _state.MaxConcurrent, totalItems,
                _state.Paused, _state.ScheduleEnabled, _state.NextWindow));
            return;
        }

        var maxConcurrent = _state.MaxConcurrent;
        var state = _state;

        Source.From(pageIds)
            .Select(id => new QueryWorkerStatus(id))
            .Ask<WorkerStatusResult>(_downloadRegion, _fanOutTimeout, 8)
            .ResumeOnError()
            .Select(r =>
            {
                var priority = LookupPriority(state, r.DownloadId);
                return new QueueItem(
                    r.DownloadId, r.Title,
                    r.Status == WorkerStatus.Downloading ? DownloadStatus.Processing : DownloadStatus.Queued,
                    r.Channel, r.HasSubtitles,
                    r.Size, r.Progress,
                    r.TotalDuration, r.Category, priority,
                    r.Phase, r.Attempt);
            })
            .RunWith(Sink.Seq<QueueItem>(), _materializer)
            .PipeTo(Sender, Self,
                success: items => new QueueResult(
                    [.. items], maxConcurrent, totalItems,
                    state.Paused, state.ScheduleEnabled, state.NextWindow),
                failure: ex => new QueueFailed(ex));
    }

    private void HandleDelete(DeleteDownload cmd)
    {
        if (!_state.Contains(cmd.DownloadId))
        {
            Sender.Tell(new DeleteDownloadResult(false, "Item not found"));
            return;
        }

        Persist(new DownloadDequeued(cmd.DownloadId), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
        });
        DeferAsync("notify", _ =>
        {
            Telemetry.Cancelled.Up();
            UpdateGauges();
            _downloadRegion.Tell(new CancelDownload(cmd.DownloadId));
            Sender.Tell(new DeleteDownloadResult(true, null));
        });
    }

    private void HandleRetry(RetryDownload cmd)
    {
        if (_state.Contains(cmd.DownloadId))
        {
            Sender.Tell(new RetryDownloadResult(false, "Item is already queued"));
            return;
        }

        Persist(new DownloadEnqueued(cmd.DownloadId, DownloadPriority.Normal.ToPersistence()), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
            _downloadRegion.Tell(new ResetDownload(cmd.DownloadId));
            _historyManager.Tell(new RemoveHistoryEntry(cmd.DownloadId), ActorRefs.NoSender);
            Sender.Tell(new RetryDownloadResult(true, null));
            DispatchNext();
        });
    }

    private void HandlePause(PauseDownloads _)
    {
        if (_state.Paused)
        {
            Sender.Tell(new PauseDownloadsResult(true));
            return;
        }

        Persist(new DownloadsPaused(), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
            Telemetry.Paused.Record(1);
            _log.Info("Downloads paused by user");
            Sender.Tell(new PauseDownloadsResult(true));
        });
    }

    private void HandleResume(ResumeDownloads _)
    {
        if (!_state.Paused)
        {
            Sender.Tell(new ResumeDownloadsResult(true));
            return;
        }

        Persist(new DownloadsResumed(), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
            Telemetry.Paused.Record(0);
            _log.Info("Downloads resumed by user");
            Sender.Tell(new ResumeDownloadsResult(true));
            DispatchNext();
        });
    }

    private void HandleForceStart(ForceStartDownload cmd)
    {
        if (_state.Dispatched.ContainsKey(cmd.DownloadId))
        {
            Sender.Tell(new ForceStartDownloadResult(false, "Item already dispatched"));
            return;
        }

        if (_state.Queued.All(e => e.Id != cmd.DownloadId))
        {
            Sender.Tell(new ForceStartDownloadResult(false, "Item not queued"));
            return;
        }

        Persist(new DownloadDispatched(cmd.DownloadId), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
            _log.Info("Force-starting download {DownloadId}", cmd.DownloadId);
        });
        DeferAsync("notify", _ =>
        {
            _downloadRegion.Tell(new StartDownload(cmd.DownloadId));
            Sender.Tell(new ForceStartDownloadResult(true, null));
        });
    }

    private void HandleMove(MoveDownload cmd)
    {
        if (_state.Queued.All(e => e.Id != cmd.DownloadId))
        {
            Sender.Tell(new MoveDownloadFailed("Item not queued"));
            return;
        }

        if (cmd.Priority is { } newPriority)
        {
            Persist(new DownloadPriorityChanged(cmd.DownloadId, newPriority.ToPersistence()), e =>
            {
                _state.Apply(e);
                Persist(new DownloadMoved(cmd.DownloadId, cmd.Position), m =>
                {
                    _state.Apply(m);
                    MaybeSnapshot();
                    _log.Info("Moved download {DownloadId} to position {Position} with priority {Priority}",
                        cmd.DownloadId, cmd.Position, newPriority);
                });
            });
            DeferAsync("notify", _ => Sender.Tell(new MoveDownloadCompleted()));
        }
        else
        {
            Persist(new DownloadMoved(cmd.DownloadId, cmd.Position), e =>
            {
                _state.Apply(e);
                MaybeSnapshot();
                _log.Info("Moved download {DownloadId} to position {Position}", cmd.DownloadId, cmd.Position);
                Sender.Tell(new MoveDownloadCompleted());
            });
        }
    }

    private void HandleSwap(SwapDownloads cmd)
    {
        var entry1 = _state.Queued.FirstOrDefault(e => e.Id == cmd.DownloadId1);
        var entry2 = _state.Queued.FirstOrDefault(e => e.Id == cmd.DownloadId2);

        if (entry1 == default || entry2 == default)
        {
            Sender.Tell(new SwapDownloadsFailed("One or both items not queued"));
            return;
        }

        if (entry1.Priority != entry2.Priority)
        {
            Sender.Tell(new SwapDownloadsFailed("Cannot swap items with different priorities"));
            return;
        }

        Persist(new DownloadSwapped(cmd.DownloadId1, cmd.DownloadId2), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
            _log.Info("Swapped downloads {Id1} and {Id2}", cmd.DownloadId1, cmd.DownloadId2);
            Sender.Tell(new SwapDownloadsCompleted());
        });
    }

    private void HandleSetPriority(SetDownloadPriority cmd)
    {
        var entry = _state.Queued.FirstOrDefault(e => e.Id == cmd.DownloadId);
        if (entry == default)
        {
            Sender.Tell(new SetDownloadPriorityFailed("Item not queued"));
            return;
        }

        if (entry.Priority == cmd.Priority)
        {
            Sender.Tell(new SetDownloadPriorityCompleted());
            return;
        }

        Persist(new DownloadPriorityChanged(cmd.DownloadId, cmd.Priority.ToPersistence()), e =>
        {
            _state.Apply(e);
            MaybeSnapshot();
            _log.Info("Changed download {DownloadId} priority to {Priority}", cmd.DownloadId, cmd.Priority);
            Sender.Tell(new SetDownloadPriorityCompleted());
            DispatchNext();
        });
    }

    private void HandleScheduleEnabled(ScheduleEnabled _)
    {
        _state.ScheduleEnabled = true;
        _state.NextWindow = null;
        _log.Info("Download schedule enabled");
        DispatchNext();
    }

    private void HandleScheduleDisabled(ScheduleDisabled msg)
    {
        _state.ScheduleEnabled = false;
        _state.NextWindow = msg.NextWindow;
        _log.Info("Download schedule disabled, next window: {NextWindow}", msg.NextWindow);
    }

    private void DispatchNext()
    {
        if (_state.Paused || !_state.ScheduleEnabled)
        {
            return;
        }

        var toDispatch = new List<Guid>();

        while (_state.Dispatched.Count + toDispatch.Count < _state.MaxConcurrent)
        {
            var dispatching = toDispatch.ToHashSet();
            var next = _state.Queued.FirstOrDefault(e =>
                !dispatching.Contains(e.Id) && e.Priority != DownloadPriority.Paused);

            if (next == default)
            {
                break;
            }

            toDispatch.Add(next.Id);
        }

        if (toDispatch.Count == 0)
        {
            return;
        }

        var events = toDispatch.Select(id => new DownloadDispatched(id)).ToArray();
        PersistAll(events, evt => _state.Apply(evt));
        UpdateGauges();

        foreach (var downloadId in toDispatch)
        {
            _log.Info("Dispatching download {DownloadId}", downloadId);
            _downloadRegion.Tell(new StartDownload(downloadId));
        }
    }

    private void UpdateGauges()
    {
        Telemetry.QueueSize.Record(_state.Queued.Count);
        Telemetry.Active.Record(_state.Dispatched.Count);
    }

    private void MaybeSnapshot()
    {
        if (LastSequenceNr % _snapshotInterval == 0)
        {
            SaveSnapshot(_state.GetPersistenceState());
        }
    }

    private static DownloadPriority LookupPriority(DownloadManagerState state, Guid downloadId)
    {
        var entry = state.Queued.FirstOrDefault(e => e.Id == downloadId);
        if (entry != default)
        {
            return entry.Priority;
        }

        return state.Dispatched.TryGetValue(downloadId, out var dispatched)
            ? dispatched.Priority
            : DownloadPriority.Normal;
    }
}
