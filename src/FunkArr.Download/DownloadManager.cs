using Akka.Actor;
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

    public override string PersistenceId => "download-manager";

    private readonly IActorRef _downloadRegion = Context.GetActor<IDownloadRegion>();
    private readonly int _maxConcurrent;
    private DownloadManagerState _state = DownloadManagerState.Empty;

    public DownloadManager(IOptionsMonitor<DownloadOptions> options)
    {
        _maxConcurrent = options.CurrentValue.ConcurrentDownloads;

        Command<AddDownload>(HandleAdd);
        Command<SlotFree>(HandleSlotFree);
        Command<QueryQueue>(HandleQueryQueue);
        Command<DeleteDownload>(HandleDelete);
        Command<RetryDownload>(HandleRetry);

        Recover<DownloadEnqueued>(evt => _state = _state.Apply(evt));
        Recover<DownloadDispatched>(evt => _state = _state.Apply(evt));
        Recover<DownloadDequeued>(evt => _state = _state.Apply(evt));
        Recover<RecoveryCompleted>(_ =>
        {
            _state = _state.ResetDispatched();
            DispatchNext();
        });
    }

    private void HandleAdd(AddDownload cmd)
    {
        var downloadId = Guid.NewGuid();

        Persist(new DownloadEnqueued(downloadId), e =>
        {
            _state = _state.Apply(e);

            _downloadRegion.Tell(new InitDownload(
                downloadId, cmd.Title, cmd.VideoUrl, cmd.SubtitleUrl,
                cmd.Channel, cmd.Duration, cmd.Size, cmd.Category));

            Sender.Tell(new DownloadAdded(downloadId));
            DispatchNext();
        });
    }

    private void HandleSlotFree(SlotFree msg)
    {
        if (!_state.Dispatched.Contains(msg.DownloadId))
        {
            return;
        }

        Persist(new DownloadDequeued(msg.DownloadId), e =>
        {
            _state = _state.Apply(e);
            DispatchNext();
        });
    }

    private void HandleQueryQueue(QueryQueue query)
    {
        var allIds = _state.Dispatched.Concat(_state.Queued).ToArray();

        if (allIds.Length == 0)
        {
            Sender.Tell(new QueueResult([], _maxConcurrent, 0));
            return;
        }

        var sender = Sender;
        var self = Self;
        var region = _downloadRegion;
        var maxConcurrent = _maxConcurrent;

        var tasks = allIds.Select(id =>
            region.Ask<WorkerStatusResult>(new QueryWorkerStatus(id), _fanOutTimeout)
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null));

        Task.WhenAll(tasks).PipeTo(sender, self, success: results =>
        {
            var items = results
                .Where(r => r is not null)
                .Select(r => new QueueItem(
                    r!.DownloadId, r.Title,
                    r.Status == (int)WorkerStatus.Downloading ? DownloadStatus.Processing : DownloadStatus.Queued,
                    r.Size, r.BytesDownloaded, r.CurrentTimeUs,
                    r.TotalDuration, r.Speed, r.Category))
                .ToArray();

            return DownloadManagerStateExtensions.PaginateQueue(items, query, maxConcurrent);
        });
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
        Persist(new DownloadEnqueued(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            _downloadRegion.Tell(new ResetDownload(cmd.DownloadId));
            sender.Tell(new RetryDownloadResult(true, null));
            DispatchNext();
        });
    }

    private void DispatchNext()
    {
        var toDispatch = new List<Guid>();

        while (_state.Dispatched.Count + toDispatch.Count < _maxConcurrent)
        {
            var dispatching = toDispatch.ToHashSet();
            var next = _state.Queued.FirstOrDefault(id => !dispatching.Contains(id));

            if (next == Guid.Empty)
            {
                break;
            }

            toDispatch.Add(next);
        }

        if (toDispatch.Count == 0)
        {
            return;
        }

        var events = toDispatch.Select(object (id) => new DownloadDispatched(id)).ToArray();
        PersistAll(events, evt => _state = _state.Apply((DownloadDispatched)evt));

        foreach (var downloadId in toDispatch)
        {
            _downloadRegion.Tell(new StartDownload(downloadId));
        }
    }

}
