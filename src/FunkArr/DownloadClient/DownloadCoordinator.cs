using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence;
using FunkArr.Muxing;
using FunkArr.Persistence;
using FunkArr.Shared;
using FunkArr.Subtitle;

namespace FunkArr.DownloadClient;

public sealed class DownloadCoordinator : ReceivePersistentActor
{
    public override string PersistenceId { get; }

    private readonly Mp4DownloadService _mp4DownloadService;
    private readonly HlsDownloadService _hlsDownloadService;
    private readonly SubtitleAcquisitionService _subtitleAcquisitionService;
    private readonly SubtitleNormalizerService _subtitleNormalizerService;
    private readonly MuxingService _muxingService;
    private readonly IFileService _fileService;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly IActorRef _trackerShard;
    private readonly IActorRef _queueCoordinatorRef;

    private string _nzoId;
    private string _videoUrl = string.Empty;
    private string? _subtitleUrl;
    private string _tempPath = string.Empty;
    private string _outputDir = string.Empty;
    private string _title = string.Empty;
    private string _stage = "Accepted";
    private string? _videoPath;
    private string? _subtitlePath;
    private IActorRef? _currentWorker;

    public DownloadCoordinator(
        Mp4DownloadService mp4DownloadService,
        HlsDownloadService hlsDownloadService,
        SubtitleAcquisitionService subtitleAcquisitionService,
        SubtitleNormalizerService subtitleNormalizerService,
        MuxingService muxingService,
        IFileService fileService,
        IActorRegistry actorRegistry)
    {
        _mp4DownloadService = mp4DownloadService;
        _hlsDownloadService = hlsDownloadService;
        _subtitleAcquisitionService = subtitleAcquisitionService;
        _subtitleNormalizerService = subtitleNormalizerService;
        _muxingService = muxingService;
        _fileService = fileService;
        _trackerShard = actorRegistry.Get<DownloadRequestTracker>();
        _queueCoordinatorRef = actorRegistry.Get<QueueCoordinator>();

        _nzoId = Context.Self.Path.Name;
        PersistenceId = $"download-{_nzoId}";

        Recovering();
    }

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(_ => Directive.Stop);
    }

    private void Recovering()
    {
        Recover<DcJobAcceptedDto>(dto => ApplyAccepted(DownloadCoordinatorEventDtoMapping.ToDomain(dto)));
        Recover<DcStageEnteredDto>(dto => ApplyStage(DownloadCoordinatorEventDtoMapping.ToDomain(dto)));
        Recover<DcJobCompletedDto>(_ => _stage = "Done");
        Recover<DcJobFailedDto>(_ => _stage = "Failed");
        Recover<DcJobCancelledDto>(_ => _stage = "Cancelled");
        Recover<RecoveryCompleted>(_ => OnRecoveryCompleted());
    }

    private void OnRecoveryCompleted()
    {
        if (_stage is "Done" or "Failed" or "Cancelled")
        {
            Become(Completed);
            return;
        }

        if (_stage == "Accepted" && string.IsNullOrEmpty(_videoUrl))
        {
            Become(WaitingForJob);
            return;
        }

        _log.Info("Recovery for {NzoId}: resuming from stage {Stage}", _nzoId, _stage);
        ResumeFromStage(_stage);
    }

    private void WaitingForJob()
    {
        Command<StartDownload>(HandleStartDownload);
        Command<CancelDownload>(_ => { });
    }

    private void HandleStartDownload(StartDownload cmd)
    {
        var evt = new DownloadCoordinatorStageEvents.JobAccepted(
            cmd.NzoId, cmd.VideoUrl, cmd.SubtitleUrl, cmd.TempPath, cmd.OutputDir, cmd.Title);

        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyAccepted(evt);
            _log.Info("Job accepted for {NzoId} '{Title}'", _nzoId, _title);
            EnterFetching();
        });
    }

    private void EnterFetching()
    {
        var evt = new DownloadCoordinatorStageEvents.StageEntered(_nzoId, "Fetching");
        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyStage(evt);
            NotifyTracker("Downloading");
            SpawnVideoWorker();
        });
    }

    private void Fetching()
    {
        Command<VideoFetchDone>(HandleVideoFetchDone);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleVideoFetchDone(VideoFetchDone msg)
    {
        _videoPath = msg.VideoPath;
        _log.Debug("Video fetched for {NzoId}: {Path}", _nzoId, _videoPath);
        EnterAcquiringSubtitle();
    }

    private void EnterAcquiringSubtitle()
    {
        var sourceType = DownloadSourceDetector.Detect(_videoUrl);
        var hlsManifestUrl = sourceType == DownloadSourceType.Hls ? _videoUrl : null;

        if (_subtitleUrl is null && hlsManifestUrl is null)
        {
            EnterMuxing();
            return;
        }

        var evt = new DownloadCoordinatorStageEvents.StageEntered(_nzoId, "AcquiringSubtitle");
        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyStage(evt);
            NotifyTracker("AcquiringSubtitle");
            SpawnSubtitleWorker(hlsManifestUrl);
        });
    }

    private void AcquiringSubtitle()
    {
        Command<SubtitleAcquireDone>(HandleSubtitleAcquireDone);
        Command<NoSubtitleAvailable>(HandleNoSubtitle);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleSubtitleAcquireDone(SubtitleAcquireDone msg)
    {
        _subtitlePath = msg.SubtitlePath;
        if (_subtitlePath is not null)
            EnterConvertingSubtitle();
        else
            EnterMuxing();
    }

    private void HandleNoSubtitle(NoSubtitleAvailable _)
    {
        _subtitlePath = null;
        EnterMuxing();
    }

    private void EnterConvertingSubtitle()
    {
        var evt = new DownloadCoordinatorStageEvents.StageEntered(_nzoId, "ConvertingSubtitle");
        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyStage(evt);
            SpawnSubtitleConvertWorker();
        });
    }

    private void ConvertingSubtitle()
    {
        Command<SubtitleConvertDone>(HandleSubtitleConvertDone);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleSubtitleConvertDone(SubtitleConvertDone msg)
    {
        _subtitlePath = msg.NormalizedPath;
        EnterMuxing();
    }

    private void EnterMuxing()
    {
        var evt = new DownloadCoordinatorStageEvents.StageEntered(_nzoId, "Muxing");
        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyStage(evt);
            NotifyTracker("Muxing");
            SpawnRemuxWorker();
        });
    }

    private void Muxing()
    {
        Command<RemuxDone>(HandleRemuxDone);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleRemuxDone(RemuxDone msg)
    {
        var evt = new DownloadCoordinatorStageEvents.JobCompleted(_nzoId, msg.OutputPath);
        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            _stage = "Done";
            _trackerShard.Tell(new DownloadRequestTracker.MarkCompleted(_nzoId, msg.OutputPath));
            _queueCoordinatorRef.Tell(new QueueCoordinator.NotifyJobFinished(_nzoId, "success"));
            _log.Info("Job completed for {NzoId}: {Path}", _nzoId, msg.OutputPath);
            Become(Completed);
        });
    }

    private void HandleWorkerFailed(WorkerFailed msg)
    {
        var evt = new DownloadCoordinatorStageEvents.JobFailed(_nzoId, msg.Kind.ToString(), msg.Reason);
        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            _stage = "Failed";
            _trackerShard.Tell(new DownloadRequestTracker.MarkFailed(_nzoId, msg.Reason));
            _queueCoordinatorRef.Tell(new QueueCoordinator.NotifyJobFinished(_nzoId, "failed"));
            _log.Warning("Job failed for {NzoId} ({Kind}): {Reason}", _nzoId, msg.Kind, msg.Reason);
            Become(Completed);
        });
    }

    private void HandleCancel(CancelDownload _)
    {
        _currentWorker?.Tell(PoisonPill.Instance);

        var evt = new DownloadCoordinatorStageEvents.JobCancelled(_nzoId);
        Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            _stage = "Cancelled";
            _trackerShard.Tell(new DownloadRequestTracker.MarkFailed(_nzoId, "Cancelled"));
            _queueCoordinatorRef.Tell(new QueueCoordinator.NotifyJobFinished(_nzoId, "cancelled"));
            _log.Info("Job cancelled for {NzoId}", _nzoId);
            Become(Completed);
        });
    }

    private void HandleWorkerTerminated(Terminated msg)
    {
        if (_currentWorker is not null && msg.ActorRef.Equals(_currentWorker))
        {
            _log.Warning("Worker terminated unexpectedly for {NzoId} in stage {Stage}", _nzoId, _stage);
        }
    }

    private void Completed()
    {
        Command<StartDownload>(_ => { });
        Command<CancelDownload>(_ => { });
        Command<VideoFetchDone>(_ => { });
        Command<SubtitleAcquireDone>(_ => { });
        Command<SubtitleConvertDone>(_ => { });
        Command<RemuxDone>(_ => { });
        Command<WorkerFailed>(_ => { });
        Command<Terminated>(_ => { });
    }

    // --- Worker spawning ---

    private void SpawnVideoWorker()
    {
        var sourceType = DownloadSourceDetector.Detect(_videoUrl);
        if (sourceType == DownloadSourceType.Hls)
        {
            _currentWorker = Context.ActorOf(Props.Create(() =>
                new HlsDownloadWorker(_hlsDownloadService, _nzoId, _videoUrl, _tempPath)), "hls-video");
        }
        else
        {
            _currentWorker = Context.ActorOf(Props.Create(() =>
                new DirectDownloadWorker(_mp4DownloadService, _nzoId, _videoUrl, _tempPath)), "direct-video");
        }

        Context.Watch(_currentWorker);
        Become(Fetching);
    }

    private void SpawnSubtitleWorker(string? hlsManifestUrl)
    {
        if (_subtitleUrl is not null)
        {
            _currentWorker = Context.ActorOf(Props.Create(() =>
                new DirectDownloadWorker(_mp4DownloadService, _nzoId, _subtitleUrl, _tempPath)), "direct-subtitle");
        }
        else if (hlsManifestUrl is not null)
        {
            _currentWorker = Context.ActorOf(Props.Create(() =>
                new SubtitleExtractWorker(_subtitleAcquisitionService, _nzoId, hlsManifestUrl, _tempPath)), "subtitle-extract");
        }

        if (_currentWorker is not null)
            Context.Watch(_currentWorker);
        Become(AcquiringSubtitle);
    }

    private void SpawnSubtitleConvertWorker()
    {
        _currentWorker = Context.ActorOf(Props.Create(() =>
            new SubtitleConvertWorker(_subtitleNormalizerService, _nzoId, _subtitlePath!, _tempPath)), "subtitle-convert");
        Context.Watch(_currentWorker);
        Become(ConvertingSubtitle);
    }

    private void SpawnRemuxWorker()
    {
        _currentWorker = Context.ActorOf(Props.Create(() =>
            new RemuxWorker(_muxingService, _nzoId, _videoPath!, _subtitlePath, _outputDir, _title)), "remux");
        Context.Watch(_currentWorker);
        Become(Muxing);
    }

    private void ResumeFromStage(string stage)
    {
        switch (stage)
        {
            case "Fetching":
                SpawnVideoWorker();
                break;
            case "AcquiringSubtitle":
                var hlsManifest = DownloadSourceDetector.Detect(_videoUrl) == DownloadSourceType.Hls ? _videoUrl : null;
                SpawnSubtitleWorker(hlsManifest);
                break;
            case "ConvertingSubtitle":
                if (_subtitlePath is not null)
                    SpawnSubtitleConvertWorker();
                else
                    EnterMuxing();
                break;
            case "Muxing":
                SpawnRemuxWorker();
                break;
            default:
                Become(WaitingForJob);
                break;
        }
    }

    private void NotifyTracker(string status)
    {
        _trackerShard.Tell(new DownloadRequestTracker.UpdateStatus(_nzoId, status));
    }

    private void ApplyAccepted(DownloadCoordinatorStageEvents.JobAccepted evt)
    {
        _nzoId = evt.NzoId;
        _videoUrl = evt.VideoUrl;
        _subtitleUrl = evt.SubtitleUrl;
        _tempPath = evt.TempPath;
        _outputDir = evt.OutputDir;
        _title = evt.Title;
        _stage = "Accepted";
    }

    private void ApplyStage(DownloadCoordinatorStageEvents.StageEntered evt)
    {
        _stage = evt.Stage;
    }
}
