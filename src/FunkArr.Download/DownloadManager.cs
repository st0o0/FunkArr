using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Download;

public sealed class DownloadManager : ReceivePersistentActor
{
    private static readonly TimeSpan _fanOutTimeout = TimeSpan.FromSeconds(2);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _downloadRegion = Context.GetActor<IDownloadRegion>();
    private int _maxConcurrent;
    private DownloadManagerState _state = DownloadManagerState.Empty;

    public override string PersistenceId => "download-manager";

    public DownloadManager(IOptionsMonitor<DownloadOptions> options)
    {
        _maxConcurrent = options.CurrentValue.ConcurrentDownloads;

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

        Recover<DownloadEnqueued>(evt => _state = _state.Apply(evt));
        Recover<DownloadDispatched>(evt => _state = _state.Apply(evt));
        Recover<DownloadDequeued>(evt => _state = _state.Apply(evt));
        Recover<DownloadMoved>(evt => _state = _state.Apply(evt));
        Recover<DownloadSwapped>(evt => _state = _state.Apply(evt));
        Recover<DownloadPriorityChanged>(evt => _state = _state.Apply(evt));
        Recover<DownloadsPaused>(evt => _state = _state.Apply(evt));
        Recover<DownloadsResumed>(evt => _state = _state.Apply(evt));
        Recover<RecoveryCompleted>(_ =>
        {
            _state = _state.ResetDispatched();
            DispatchNext();
        });

        options.OnChange(opts =>
        {
            _maxConcurrent = opts.ConcurrentDownloads;
            DispatchNext();
        });
    }

    private void HandleAdd(AddDownload cmd)
    {
        var downloadId = Guid.NewGuid();

        _log.Info("Enqueuing download {DownloadId}: {Title}", downloadId, cmd.Title);
        Persist(new DownloadEnqueued(downloadId, cmd.Priority.ToPersistence()), e =>
        {
            _state = _state.Apply(e);

            _downloadRegion.Tell(new InitDownload(
                downloadId, cmd.Title, cmd.VideoUrl, cmd.SubtitleUrl,
                cmd.Channel, cmd.Duration, cmd.Size, cmd.Category));

            Sender.Tell(new DownloadAdded(downloadId));
            _log.Debug("Queue depth: {Queued} queued, {Dispatched} dispatched", _state.Queued.Count, _state.Dispatched.Count);
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
            _state = _state.Apply(e);
            _log.Debug("Queue depth: {Queued} queued, {Dispatched} dispatched", _state.Queued.Count, _state.Dispatched.Count);
            DispatchNext();
        });
    }

    private void HandleQueryQueue(QueryQueue query)
    {
        var allIds = _state.Dispatched.Keys
            .Concat(_state.Queued.Select(e => e.Id))
            .ToArray();

        if (allIds.Length == 0)
        {
            Sender.Tell(new QueueResult([], _maxConcurrent, 0,
                _state.Paused, _state.ScheduleEnabled, _state.NextWindow));
            return;
        }

        var sender = Sender;
        var self = Self;
        var region = _downloadRegion;
        var maxConcurrent = _maxConcurrent;
        var state = _state;

        var tasks = allIds.Select(id =>
            region.Ask<WorkerStatusResult>(new QueryWorkerStatus(id), _fanOutTimeout)
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null));

        Task.WhenAll(tasks).PipeTo(sender, self, success: results =>
        {
            var items = results
                .Where(r => r is not null)
                .Select(r =>
                {
                    var priority = LookupPriority(state, r!.DownloadId);
                    return new QueueItem(
                        r.DownloadId, r.Title,
                        r.Status == (int)WorkerStatus.Downloading ? DownloadStatus.Processing : DownloadStatus.Queued,
                        r.Channel, r.HasSubtitles,
                        r.Size, r.BytesDownloaded, r.CurrentTimeUs,
                        r.TotalDuration, r.Speed, r.Category, priority);
                })
                .ToArray();

            return DownloadManagerStateExtensions.PaginateQueue(items, query, maxConcurrent, state);
        }, failure: ex => new QueueFailed(ex));
    }

    private void HandleDelete(DeleteDownload cmd)
    {
        if (!_state.Contains(cmd.DownloadId))
        {
            Sender.Tell(new DeleteDownloadResult(false, "Item not found"));
            return;
        }

        var sender = Sender;
        Persist(new DownloadDequeued(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            _downloadRegion.Tell(new CancelDownload(cmd.DownloadId));
            sender.Tell(new DeleteDownloadResult(true, null));
        });
    }

    private void HandleRetry(RetryDownload cmd)
    {
        if (_state.Contains(cmd.DownloadId))
        {
            Sender.Tell(new RetryDownloadResult(false, "Item is already queued"));
            return;
        }

        var sender = Sender;
        Persist(new DownloadEnqueued(cmd.DownloadId, DownloadPriority.Normal.ToPersistence()), e =>
        {
            _state = _state.Apply(e);
            _downloadRegion.Tell(new ResetDownload(cmd.DownloadId));
            sender.Tell(new RetryDownloadResult(true, null));
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

        var sender = Sender;
        Persist(new DownloadsPaused(), e =>
        {
            _state = _state.Apply(e);
            _log.Info("Downloads paused by user");
            sender.Tell(new PauseDownloadsResult(true));
        });
    }

    private void HandleResume(ResumeDownloads _)
    {
        if (!_state.Paused)
        {
            Sender.Tell(new ResumeDownloadsResult(true));
            return;
        }

        var sender = Sender;
        Persist(new DownloadsResumed(), e =>
        {
            _state = _state.Apply(e);
            _log.Info("Downloads resumed by user");
            sender.Tell(new ResumeDownloadsResult(true));
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

        if (!_state.Queued.Any(e => e.Id == cmd.DownloadId))
        {
            Sender.Tell(new ForceStartDownloadResult(false, "Item not queued"));
            return;
        }

        var sender = Sender;
        Persist(new DownloadDispatched(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            _log.Info("Force-starting download {DownloadId}", cmd.DownloadId);
            _downloadRegion.Tell(new StartDownload(cmd.DownloadId));
            sender.Tell(new ForceStartDownloadResult(true, null));
        });
    }

    private void HandleMove(MoveDownload cmd)
    {
        if (!_state.Queued.Any(e => e.Id == cmd.DownloadId))
        {
            Sender.Tell(new MoveDownloadFailed("Item not queued"));
            return;
        }

        var sender = Sender;

        if (cmd.Priority is { } newPriority)
        {
            var prioEvt = new DownloadPriorityChanged(cmd.DownloadId, newPriority.ToPersistence());
            Persist(prioEvt, e =>
            {
                _state = _state.Apply(e);
                Persist(new DownloadMoved(cmd.DownloadId, cmd.Position), m =>
                {
                    _state = _state.Apply(m);
                    _log.Info("Moved download {DownloadId} to position {Position} with priority {Priority}", cmd.DownloadId, cmd.Position, newPriority);
                    sender.Tell(new MoveDownloadCompleted());
                });
            });
        }
        else
        {
            Persist(new DownloadMoved(cmd.DownloadId, cmd.Position), e =>
            {
                _state = _state.Apply(e);
                _log.Info("Moved download {DownloadId} to position {Position}", cmd.DownloadId, cmd.Position);
                sender.Tell(new MoveDownloadCompleted());
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

        var sender = Sender;
        Persist(new DownloadSwapped(cmd.DownloadId1, cmd.DownloadId2), e =>
        {
            _state = _state.Apply(e);
            _log.Info("Swapped downloads {Id1} and {Id2}", cmd.DownloadId1, cmd.DownloadId2);
            sender.Tell(new SwapDownloadsCompleted());
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

        var sender = Sender;
        Persist(new DownloadPriorityChanged(cmd.DownloadId, cmd.Priority.ToPersistence()), e =>
        {
            _state = _state.Apply(e);
            _log.Info("Changed download {DownloadId} priority to {Priority}", cmd.DownloadId, cmd.Priority);
            sender.Tell(new SetDownloadPriorityCompleted());
            DispatchNext();
        });
    }

    private void HandleScheduleEnabled(ScheduleEnabled _)
    {
        _state = _state with { ScheduleEnabled = true, NextWindow = null };
        _log.Info("Download schedule enabled");
        DispatchNext();
    }

    private void HandleScheduleDisabled(ScheduleDisabled msg)
    {
        _state = _state with { ScheduleEnabled = false, NextWindow = msg.NextWindow };
        _log.Info("Download schedule disabled, next window: {NextWindow}", msg.NextWindow);
    }

    private void DispatchNext()
    {
        if (_state.Paused || !_state.ScheduleEnabled)
        {
            return;
        }

        var toDispatch = new List<Guid>();

        while (_state.Dispatched.Count + toDispatch.Count < _maxConcurrent)
        {
            var dispatching = toDispatch.ToHashSet();
            var next = _state.Queued.FirstOrDefault(e => !dispatching.Contains(e.Id));

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

        var events = toDispatch.Select(object (id) => new DownloadDispatched(id)).ToArray();
        PersistAll(events, evt => _state = _state.Apply((DownloadDispatched)evt));

        foreach (var downloadId in toDispatch)
        {
            _log.Info("Dispatching download {DownloadId}", downloadId);
            _downloadRegion.Tell(new StartDownload(downloadId));
        }
    }

    private static DownloadPriority LookupPriority(DownloadManagerState state, Guid downloadId)
    {
        var entry = state.Queued.FirstOrDefault(e => e.Id == downloadId);
        if (entry != default)
        {
            return entry.Priority;
        }

        return state.Dispatched.TryGetValue(downloadId, out var priority)
            ? priority
            : DownloadPriority.Normal;
    }
}
