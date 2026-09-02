using Akka.Actor;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Download;

public sealed class DownloadManager : ReceivePersistentActor, IWithUnboundedStash
{
    public override string PersistenceId => "download-manager";

    private readonly IActorRef _downloadRegion = Context.GetActor<IDownloadRegion>();
    private readonly int _maxConcurrent;
    private readonly string _downloadPath;
    private DownloadManagerState _state = DownloadManagerState.Empty;

    public new IStash Stash { get; set; } = null!;

    public DownloadManager(IOptionsMonitor<FunkArrOptions> options, int maxConcurrent = 3)
    {
        _maxConcurrent = maxConcurrent;
        _downloadPath = options.CurrentValue.DownloadPath;

        Command<AddDownload>(HandleAdd);
        Command<DownloadProgress>(HandleProgress);
        Command<DownloadCompleted>(HandleCompleted);
        Command<DownloadFailed>(HandleFailed);
        Command<QueryQueue>(HandleQueryQueue);
        Command<QueryHistory>(HandleQueryHistory);
        Command<DeleteDownload>(HandleDelete);
        Command<RetryDownload>(HandleRetry);

        Recover<DownloadQueued>(evt => _state = _state.Apply(evt));
        Recover<DownloadStatusChanged>(evt => _state = _state.Apply(evt));
        Recover<DownloadRemoved>(evt => _state = _state.Apply(evt));
        Recover<RecoveryCompleted>(_ =>
        {
            _state = _state.RequeueProcessing();
            DispatchNext();
        });
    }

    private void HandleAdd(AddDownload cmd)
    {
        var downloadId = Guid.NewGuid();
        var evt = new DownloadQueued(
            downloadId, cmd.Title, cmd.VideoUrl, cmd.SubtitleUrl,
            cmd.Channel, cmd.Duration, cmd.Size, cmd.Category);

        Persist(evt, e =>
        {
            _state = _state.Apply(e);
            Sender.Tell(new DownloadAdded(downloadId));
            DispatchNext();
        });
    }

    private void HandleProgress(DownloadProgress msg)
        => _state = _state.UpdateProgress(msg.DownloadId, msg.BytesDownloaded, msg.CurrentTimeUs, msg.Speed);

    private void HandleCompleted(DownloadCompleted msg)
    {
        var evt = new DownloadStatusChanged(
            msg.DownloadId, (int)DownloadStatus.Completed, msg.FilePath,
            msg.DownloadTimeSeconds, null, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        Persist(evt, e =>
        {
            _state = _state.Apply(e);
            DispatchNext();
        });
    }

    private void HandleFailed(DownloadFailed msg)
    {
        var evt = new DownloadStatusChanged(
            msg.DownloadId, (int)DownloadStatus.Failed, null,
            0, msg.Reason, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        Persist(evt, e =>
        {
            _state = _state.Apply(e);
            DispatchNext();
        });
    }

    private void HandleQueryQueue(QueryQueue _) =>
        Sender.Tell(_state.ToQueueResult());

    private void HandleQueryHistory(QueryHistory _) =>
        Sender.Tell(_state.ToHistoryResult());

    private void HandleDelete(DeleteDownload cmd)
    {
        var inQueue = _state.Queue.Any(e => e.DownloadId == cmd.DownloadId);
        var inHistory = _state.History.Any(e => e.DownloadId == cmd.DownloadId);

        if (!inQueue && !inHistory)
        {
            Sender.Tell(new DeleteDownloadResult(false, "Item not found"));
            return;
        }

        var sender = Sender;
        Persist(new DownloadRemoved(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            sender.Tell(new DeleteDownloadResult(true, null));
        });
    }

    private void HandleRetry(RetryDownload cmd)
    {
        var historyItem = _state.History.FirstOrDefault(e => e.DownloadId == cmd.DownloadId);
        if (historyItem is null)
        {
            Sender.Tell(new RetryDownloadResult(false, "Item not found"));
            return;
        }

        if (historyItem.Status != DownloadStatus.Failed)
        {
            Sender.Tell(new RetryDownloadResult(false, "Item is not failed"));
            return;
        }

        var sender = Sender;
        var removed = new DownloadRemoved(cmd.DownloadId);
        Persist(removed, e1 =>
        {
            _state = _state.Apply(e1);

            var queued = new DownloadQueued(
                cmd.DownloadId, historyItem.Title, "", null,
                "", 0, historyItem.Size, historyItem.Category);

            Persist(queued, e2 =>
            {
                _state = _state.Apply(e2);
                sender.Tell(new RetryDownloadResult(true, null));
                DispatchNext();
            });
        });
    }

    private void DispatchNext()
    {
        var toDispatch = new List<(DownloadEntry Entry, StartDownload Cmd, DownloadStatusChanged Evt)>();

        while (_state.ActiveCount() + toDispatch.Count < _maxConcurrent)
        {
            var dispatching = toDispatch.Select(d => d.Entry.DownloadId).ToHashSet();
            var next = _state.Queue.FirstOrDefault(e =>
                e.Status == DownloadStatus.Queued && !dispatching.Contains(e.DownloadId));
            if (next is null)
            {
                break;
            }

            var outputPath = Path.Combine(_downloadPath, SanitizeFilename(next.Title) + ".mkv");
            var startCmd = new StartDownload(
                next.DownloadId, next.Title, next.VideoUrl, next.SubtitleUrl,
                next.Channel, next.Duration, next.Size, outputPath);
            var statusEvt = new DownloadStatusChanged(
                next.DownloadId, (int)DownloadStatus.Processing, null, 0, null, 0);

            toDispatch.Add((next, startCmd, statusEvt));
        }

        if (toDispatch.Count == 0)
        {
            return;
        }

        var events = toDispatch.Select(object (d) => d.Evt).ToArray();
        PersistAll(events, evt => { _state = _state.Apply((DownloadStatusChanged)evt); });

        foreach (var (_, cmd, _) in toDispatch)
        {
            _downloadRegion.Tell(cmd);
        }
    }

    private static string SanitizeFilename(string title) =>
        string.Concat(title.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
