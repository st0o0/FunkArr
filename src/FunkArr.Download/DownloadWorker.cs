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
    private readonly DownloadOptions _downloadOptions;
    private DownloadWorkerState _state = DownloadWorkerState.Empty;
    private CancellationTokenSource? _cts;
    private string? _incompletePath;
    private string? _outputPath;

    public DownloadWorker(IOptionsMonitor<DownloadOptions> options)
    {
        _downloadOptions = options.CurrentValue;

        Command<InitDownload>(HandleInit);
        Command<StartDownload>(HandleStart);
        Command<CancelDownload>(HandleCancel);
        Command<ResetDownload>(HandleReset);
        Command<QueryWorkerStatus>(HandleQueryStatus);
        Command<ProgressUpdate>(HandleProgress);
        Command<ProcessExited>(HandleExited);

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

        ComputePaths();
        Directory.CreateDirectory(_incompletePath!);

        Persist(new DownloadStarted(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            _cts = FfmpegRunner.Run(Self, _state.VideoUrl!, _state.SubtitleUrl, _outputPath!);
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
            _state.Size,
            (int)_state.Status,
            _state.BytesDownloaded,
            _state.CurrentTimeUs,
            _state.Duration,
            _state.Speed,
            _outputPath,
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
        };
    }

    private void HandleExited(ProcessExited msg)
    {
        if (!_state.IsInitialized)
        {
            return;
        }

        var downloadId = Guid.Parse(Context.Self.Path.Name);

        if (msg.ExitCode == 0)
        {
            var outputDir = Path.GetDirectoryName(_outputPath!);
            if (outputDir is not null)
            {
                Directory.CreateDirectory(outputDir);
            }

            var completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var evt = new DownloadSucceeded(
                downloadId, _outputPath!, msg.ElapsedSeconds, completedAt);

            Persist(evt, e =>
            {
                _state = _state.Apply(e);
                CleanupIncomplete();
                _downloadManager.Tell(new SlotFree(downloadId));
                _downloadHistory.Tell(new RecordDownload(
                    downloadId, _state.Title!, _state.Category!, _state.Size,
                    DownloadStatus.Completed, _outputPath!, null,
                    msg.ElapsedSeconds, completedAt));
                Passivate();
            });
        }
        else if (_state.SubtitleUrl is not null && IsSubtitleError(msg.ErrorOutput))
        {
            _state = _state with { SubtitleUrl = null };
            _cts = FfmpegRunner.Run(Self, _state.VideoUrl!, null, _outputPath!);
        }
        else
        {
            var reason = TruncateError(msg.ErrorOutput ?? "FFmpeg exited with code " + msg.ExitCode);
            var completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var evt = new DownloadFaulted(downloadId, reason);

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

    private void ComputePaths()
    {
        var entityId = Context.Self.Path.Name;
        _incompletePath = Path.GetFullPath(Path.Combine(_downloadOptions.IncompletePath, entityId));

        var categoryDir = _downloadOptions.ResolveCategoryDir(_state.Category);
        var outputDir = string.IsNullOrEmpty(categoryDir)
            ? Path.Combine(_downloadOptions.CompletePath, _state.Title!)
            : Path.Combine(_downloadOptions.CompletePath, categoryDir, _state.Title!);
        _outputPath = Path.GetFullPath(Path.Combine(outputDir, _state.Title! + ".mkv"));
    }

    private void CleanupIncomplete()
    {
        if (_incompletePath is null)
        {
            return;
        }

        try
        {
            if (Directory.Exists(_incompletePath))
            {
                Directory.Delete(_incompletePath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _log.Warning("Failed to clean up incomplete directory {Path}: {Error}",
                _incompletePath, ex.Message);
        }
    }

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
        if (_state.IsInitialized)
        {
            ComputePaths();
        }

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

    private static bool IsSubtitleError(string? stderr) =>
        stderr is not null && (
            stderr.Contains("subtitle", StringComparison.OrdinalIgnoreCase) ||
            stderr.Contains("Stream map", StringComparison.OrdinalIgnoreCase));

    private static string TruncateError(string error) =>
        error.Length > 500 ? error[^500..] : error;
}
