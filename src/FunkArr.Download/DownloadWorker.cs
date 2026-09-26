using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Download;

public sealed class DownloadWorker : ReceivePersistentActor, IWithTimers
{
    private static readonly TimeSpan _maxBackoff = TimeSpan.FromMinutes(5);
    private const string _retryTimerKey = "retry";

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _downloadManager = Context.GetActor<IDownloadManager>();
    private readonly IActorRef _downloadHistory = Context.GetActor<IDownloadHistoryManager>();
    private readonly IRemuxer _remuxer;
    private readonly IDataFiles _dataFiles;
    private readonly DataPaths _dataPaths;
    private readonly IOptionsMonitor<DownloadOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;
    private readonly Guid _downloadId;
    private DownloadWorkerState _state = DownloadWorkerState.Empty;
    private CancellationTokenSource? _cts;

    public ITimerScheduler Timers { get; set; } = null!;
    public override string PersistenceId { get; }

    private sealed record RetryAttempt;

    public DownloadWorker(
        IRemuxer remuxer, IDataFiles dataFiles, DataPaths dataPaths,
        IOptionsMonitor<DownloadOptions> options, TimeProvider timeProvider, string entityId)
    {
        _downloadId = Guid.Parse(entityId);
        PersistenceId = $"download-{entityId}";
        _remuxer = remuxer;
        _dataFiles = dataFiles;
        _dataPaths = dataPaths;
        _optionsMonitor = options;
        _timeProvider = timeProvider;

        Command<InitDownload>(HandleInit);
        Command<StartDownload>(HandleStart);
        Command<CancelDownload>(HandleCancel);
        Command<ResetDownload>(HandleReset);
        Command<QueryWorkerStatus>(HandleQueryStatus);
        Command<ProgressUpdate>(HandleProgress);
        Command<FfmpegResult>(HandleFfmpegResult);
        Command<RetryAttempt>(_ => HandleRetry());

        Recover<DownloadInitialized>(evt => _state = _state.Apply(evt));
        Recover<DownloadStarted>(evt => _state = _state.Apply(evt));
        Recover<DownloadSucceeded>(evt => _state = _state.Apply(evt));
        Recover<DownloadFaulted>(evt => _state = _state.Apply(evt));
        Recover<DownloadAttemptStarted>(evt => _state = _state.Apply(evt));
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
            cmd.Channel, cmd.Duration, cmd.Size, cmd.Category.ToPersistence(),
            cmd.RouteName, cmd.ProxyUrl);

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
            Persist(new DownloadFaulted(cmd.DownloadId, "Video URL is empty", Persistence.PersistedFailureKind.Permanent),
                e => _state = _state.Apply(e));
            DeferAsync("notify", _ =>
            {
                _downloadManager.Tell(new SlotFree(cmd.DownloadId));
                _downloadHistory.Tell(_state.ToRecordDownload(_downloadId, _timeProvider, 0, null));
            });
            return;
        }

        var paths = ResolvePaths();
        _dataFiles.CreateDirectory(Path.GetDirectoryName(paths.IncompletePath)!);

        _log.Info("Download {DownloadId} started: {Title}", cmd.DownloadId, _state.Title);
        var attemptEvt = new DownloadAttemptStarted(cmd.DownloadId, 1);
        Persist(attemptEvt, e => _state = _state.Apply(e));
        DeferAsync("start", _ => StartFfmpeg(paths.IncompletePath));
    }

    private void HandleCancel(CancelDownload _) => CancelRunning();

    private void HandleReset(ResetDownload _)
    {
        if (_state.Status is not (WorkerStatus.Failed or WorkerStatus.Completed))
        {
            return;
        }

        Timers.Cancel(_retryTimerKey);

        var evt = new DownloadInitialized(
            _downloadId,
            _state.Title!, _state.VideoUrl!, _state.SubtitleUrl,
            _state.Channel!, _state.Duration, _state.Size,
            _state.Category!.Value.ToPersistence(),
            _state.RouteName, _state.ProxyUrl);

        Persist(evt, e => _state = _state.Apply(e));
    }

    private void HandleQueryStatus(QueryWorkerStatus _)
    {
        if (!_state.IsInitialized)
        {
            return;
        }

        Sender.Tell(new WorkerStatusResult(
            _downloadId,
            _state.Title!,
            _state.Category!.Value,
            _state.Channel ?? "",
            _state.SubtitleUrl is not null,
            _state.Size,
            _state.Status,
            _state.BytesDownloaded,
            _state.CurrentTimeUs,
            _state.Duration,
            _state.Speed,
            _state.FailMessage,
            _state.Phase,
            _state.Attempt));
    }

    private void HandleProgress(ProgressUpdate msg)
    {
        if (!_state.IsInitialized)
        {
            return;
        }

        var phase = DownloadPhaseExtensions.DerivePhase(msg.TotalSize, _state.Size, msg.OutTimeUs);

        _state = _state with
        {
            BytesDownloaded = msg.TotalSize,
            CurrentTimeUs = msg.OutTimeUs,
            Speed = msg.Speed,
            Size = Math.Max(_state.Size, msg.TotalSize),
            Phase = phase,
        };
    }

    private void HandleFfmpegResult(FfmpegResult msg)
    {
        if (!_state.IsInitialized)
        {
            return;
        }

        var paths = ResolvePaths();

        if (msg.Success)
        {
            _state = _state with { Phase = DownloadPhase.Moving };

            try
            {
                _dataFiles.CreateDirectory(Path.GetDirectoryName(paths.CompletePath)!);
                _dataFiles.Move(paths.IncompletePath, paths.CompletePath);
            }
            catch (Exception ex)
            {
                Telemetry.MoveFailed.Add(1);
                _log.Warning(ex, "Download {DownloadId} move failed: {Title}", _downloadId, _state.Title);
                msg = msg with { Success = false, Error = $"Move failed: {ex.Message}" };
            }
        }

        if (msg.Success)
        {
            var completedAt = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
            var evt = new DownloadSucceeded(_downloadId, msg.ElapsedSeconds, completedAt);

            _log.Info("Download {DownloadId} completed in {Elapsed}s: {Title}", _downloadId, msg.ElapsedSeconds, _state.Title);
            Persist(evt, e => _state = _state.Apply(e));
            DeferAsync("notify", _ =>
            {
                _dataFiles.Remove(Path.GetDirectoryName(paths.IncompletePath)!);
                _downloadManager.Tell(new SlotFree(_downloadId));
                _downloadHistory.Tell(_state.ToRecordDownload(_downloadId, _timeProvider, msg.ElapsedSeconds, paths));
            });
        }
        else
        {
            var reason = msg.Error ?? "FFmpeg failed";
            var evt = new DownloadFaulted(_downloadId, reason, msg.FailureKind.ToPersistence());

            _log.Warning("Download {DownloadId} failed: {Reason} - {Title}", _downloadId, reason, _state.Title);
            Persist(evt, e => _state = _state.Apply(e));

            if (ShouldRetry(msg.FailureKind))
            {
                var delay = CalculateBackoff(_state.Attempt);
                _log.Info("Download {DownloadId} scheduling retry {Attempt} in {Delay}s", _downloadId, _state.Attempt + 1, delay.TotalSeconds);
                Timers.StartSingleTimer(_retryTimerKey, new RetryAttempt(), delay);
            }
            else
            {
                DeferAsync("notify", _ =>
                {
                    _downloadManager.Tell(new SlotFree(_downloadId));
                    _downloadHistory.Tell(_state.ToRecordDownload(_downloadId, _timeProvider, 0, null));
                });
            }
        }
    }

    private void HandleRetry()
    {
        if (!_state.IsInitialized || _state.Status != WorkerStatus.Failed)
        {
            return;
        }

        var paths = ResolvePaths();
        _dataFiles.CreateDirectory(Path.GetDirectoryName(paths.IncompletePath)!);

        var attempt = _state.Attempt + 1;
        Telemetry.Retries.Add(1, new KeyValuePair<string, object?>("reason",
            _state.LastFailureKind?.ToString().ToLowerInvariant() ?? "unknown"));
        _log.Info("Download {DownloadId} retry attempt {Attempt}: {Title}", _downloadId, attempt, _state.Title);
        var evt = new DownloadAttemptStarted(_downloadId, attempt);
        Persist(evt, e => _state = _state.Apply(e));
        DeferAsync("start", _ => StartFfmpeg(paths.IncompletePath));
    }

    private bool ShouldRetry(FailureKind failureKind)
    {
        var opts = _optionsMonitor.CurrentValue;
        return opts.RetryEnabled
            && failureKind == FailureKind.Transient
            && _state.Attempt < opts.MaxRetries;
    }

    private TimeSpan CalculateBackoff(int attempt)
    {
        var opts = _optionsMonitor.CurrentValue;
        var delay = opts.RetryBackoffBase * Math.Pow(2, Math.Max(0, attempt - 1));
        return delay > _maxBackoff ? _maxBackoff : delay;
    }

    private void StartFfmpeg(string outputPath)
    {
        _cts = new CancellationTokenSource();
        var self = Self;
        _remuxer.RunAsync(_state.VideoUrl!, _state.SubtitleUrl, outputPath,
            _state.RouteName, _state.ProxyUrl, self.Tell, _cts.Token)
            .PipeTo(self, failure: ex => new FfmpegResult(false, -1, ex.Message, 0));
    }

    private DataPaths.ResolvedDownload ResolvePaths() =>
        _dataPaths.ResolveDownload(_downloadId.ToString(), _state.Title!, _state.Category?.ToString().ToLowerInvariant(), _optionsMonitor.CurrentValue.Categories);

    private void CancelRunning()
    {
        Timers.Cancel(_retryTimerKey);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnRecoveryCompleted()
    {
        if (_state.Phase.IsTransient())
        {
            _state = _state with
            {
                Status = WorkerStatus.Initialized,
                Phase = DownloadPhase.Initialized,
            };
        }
    }

    protected override void PostStop() => CancelRunning();
}
