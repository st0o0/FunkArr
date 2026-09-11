using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Download;

public sealed class DownloadWorker : ReceivePersistentActor
{
    public override string PersistenceId => "download-" + Context.Self.Path.Name;

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _downloadManager = Context.GetActor<IDownloadManager>();
    private readonly IActorRef _downloadHistory = Context.GetActor<IDownloadHistoryManager>();
    private readonly IRemuxer _remuxer;
    private readonly IDataFiles _dataFiles;
    private readonly DataPaths _dataPaths;
    private readonly DownloadOptions _options;
    private DownloadWorkerState _state = DownloadWorkerState.Empty;
    private CancellationTokenSource? _cts;

    public DownloadWorker(IRemuxer remuxer, IDataFiles dataFiles, DataPaths dataPaths, IOptions<DownloadOptions> options)
    {
        _remuxer = remuxer;
        _dataFiles = dataFiles;
        _dataPaths = dataPaths;
        _options = options.Value;

        Command<InitDownload>(HandleInit);
        Command<StartDownload>(HandleStart);
        Command<CancelDownload>(HandleCancel);
        Command<ResetDownload>(HandleReset);
        Command<QueryWorkerStatus>(HandleQueryStatus);
        Command<ProgressUpdate>(HandleProgress);
        Command<FfmpegResult>(HandleFfmpegResult);

        Recover<DownloadInitialized>(evt => _state = _state.Apply(evt));
        Recover<DownloadStarted>(evt => _state = _state.Apply(evt));
        Recover<DownloadSucceeded>(evt => _state = _state.Apply(evt));
        Recover<DownloadFaulted>(evt => _state = _state.Apply(evt));
        Recover<RecoveryCompleted>(_ => OnRecoveryCompleted());
    }

    private void HandleInit(InitDownload cmd)
    {
        if (_state.IsInitialized)
        {
            return;
        }

        var evt = new DownloadInitialized(
            cmd.DownloadId, cmd.Title, cmd.VideoUrl, cmd.SubtitleUrl,
            cmd.Channel, cmd.Duration, cmd.Size, cmd.Category);

        Persist(evt, e => _state = _state.Apply(e));
    }

    private void HandleStart(StartDownload cmd)
    {
        if (!_state.IsInitialized || _state.Status != WorkerStatus.Initialized)
        {
            return;
        }

        if (string.IsNullOrEmpty(_state.VideoUrl))
        {
            _log.Warning("Download {DownloadId} failed - video URL is empty: {Title}", cmd.DownloadId, _state.Title);
            var reason = "Video URL is empty";
            var completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Persist(new DownloadFaulted(cmd.DownloadId, reason), e =>
            {
                _state = _state.Apply(e);
                _downloadManager.Tell(new SlotFree(cmd.DownloadId));
                _downloadHistory.Tell(new RecordDownload(
                    cmd.DownloadId, _state.Title!, _state.Category!, _state.Size,
                    DownloadStatus.Failed, null, reason, 0, completedAt));
                Passivate();
            });
            return;
        }

        var paths = ResolvePaths();
        _dataFiles.CreateDirectory(Path.GetDirectoryName(paths.IncompletePath)!);

        _log.Info("Download {DownloadId} started: {Title}", cmd.DownloadId, _state.Title);
        Persist(new DownloadStarted(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            StartFfmpeg(_state.VideoUrl!, _state.SubtitleUrl, paths.IncompletePath);
        });
    }

    private void HandleCancel(CancelDownload _)
    {
        CancelRunning();
        Passivate();
    }

    private void HandleReset(ResetDownload _)
    {
        if (_state.Status != WorkerStatus.Failed)
        {
            return;
        }

        var evt = new DownloadInitialized(
            Guid.Parse(Context.Self.Path.Name),
            _state.Title!, _state.VideoUrl!, _state.SubtitleUrl,
            _state.Channel!, _state.Duration, _state.Size,
            _state.Category!);

        Persist(evt, e => _state = _state.Apply(e));
    }

    private void HandleQueryStatus(QueryWorkerStatus _)
    {
        if (!_state.IsInitialized)
        {
            return;
        }

        Sender.Tell(new WorkerStatusResult(
            Guid.Parse(Context.Self.Path.Name),
            _state.Title!,
            _state.Category!,
            _state.Channel ?? "",
            _state.SubtitleUrl is not null,
            _state.Size,
            (int)_state.Status,
            _state.BytesDownloaded,
            _state.CurrentTimeUs,
            _state.Duration,
            _state.Speed,
            _state.FailMessage));
    }

    private void HandleProgress(ProgressUpdate msg)
    {
        if (!_state.IsInitialized)
        {
            return;
        }

        _state = _state with
        {
            BytesDownloaded = msg.TotalSize,
            CurrentTimeUs = msg.OutTimeUs,
            Speed = msg.Speed,
            Size = Math.Max(_state.Size, msg.TotalSize),
        };
    }

    private void HandleFfmpegResult(FfmpegResult msg)
    {
        if (!_state.IsInitialized)
        {
            return;
        }

        var downloadId = Guid.Parse(Context.Self.Path.Name);
        var paths = ResolvePaths();

        if (msg.Success)
        {
            try
            {
                _dataFiles.CreateDirectory(Path.GetDirectoryName(paths.CompletePath)!);
                _dataFiles.Move(paths.IncompletePath, paths.CompletePath);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Download {DownloadId} move failed: {Title}", downloadId, _state.Title);
                msg = msg with { Success = false, Error = $"Move failed: {ex.Message}" };
            }
        }

        if (msg.Success)
        {
            var completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var evt = new DownloadSucceeded(downloadId, msg.ElapsedSeconds, completedAt);

            _log.Info("Download {DownloadId} completed in {Elapsed}s: {Title}", downloadId, msg.ElapsedSeconds, _state.Title);
            Persist(evt, e =>
            {
                _state = _state.Apply(e);
                _dataFiles.Remove(Path.GetDirectoryName(paths.IncompletePath)!);
                _downloadManager.Tell(new SlotFree(downloadId));
                _downloadHistory.Tell(new RecordDownload(
                    downloadId, _state.Title!, _state.Category!, _state.Size,
                    DownloadStatus.Completed, paths.RelativePath, null,
                    msg.ElapsedSeconds, completedAt));
                Passivate();
            });
        }
        else
        {
            var reason = msg.Error ?? "FFmpeg failed";
            var completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var evt = new DownloadFaulted(downloadId, reason);

            _log.Warning("Download {DownloadId} failed: {Reason} - {Title}", downloadId, reason, _state.Title);
            Persist(evt, e =>
            {
                _state = _state.Apply(e);
                _downloadManager.Tell(new SlotFree(downloadId));
                _downloadHistory.Tell(new RecordDownload(
                    downloadId, _state.Title!, _state.Category!, _state.Size,
                    DownloadStatus.Failed, null, reason, 0, completedAt));
                Passivate();
            });
        }
    }

    private void StartFfmpeg(string videoUrl, string? subtitleUrl, string outputPath)
    {
        _cts = new CancellationTokenSource();
        var self = Self;
        _remuxer.RunAsync(videoUrl, subtitleUrl, outputPath,
            progress => self.Tell(progress), _cts.Token)
            .PipeTo(self, failure: ex => new FfmpegResult(false, -1, ex.Message, 0));
    }

    private DataPaths.ResolvedDownload ResolvePaths() =>
        _dataPaths.ResolveDownload(Context.Self.Path.Name, _state.Title!, _state.Category, _options.Categories);

    private void CancelRunning()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void Passivate()
    {
        CancelRunning();
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }

    private void OnRecoveryCompleted()
    {
        switch (_state.Status)
        {
            case WorkerStatus.Downloading:
                _state = _state with { Status = WorkerStatus.Initialized };
                break;
            case WorkerStatus.Completed:
            case WorkerStatus.Failed:
                Passivate();
                break;
        }
    }

    protected override void PostStop() => CancelRunning();
}
