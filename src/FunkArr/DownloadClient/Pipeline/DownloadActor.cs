using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence;
using FunkArr.DownloadClient.Queue;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Persistence;
using FunkArr.Shared;

namespace FunkArr.DownloadClient.Pipeline;

public sealed class DownloadActor : ReceivePersistentActor
{
    public override string PersistenceId { get; }

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IFileService _fileService;

    private readonly IActorRef _trackerShard;
    private readonly IActorRef _queueActorRef;

    private string _nzoId;
    private string _videoUrl = string.Empty;
    private string? _subtitleUrl;
    private string _title = string.Empty;
    private string? _category;
    private string _stage = "Accepted";
    private bool _hasSubtitle;
    private IActorRef? _currentWorker;

    public DownloadActor(IActorRegistry actorRegistry, IFileService fileService)
    {
        _trackerShard = actorRegistry.Get<DownloadRequestActor>();
        _queueActorRef = actorRegistry.Get<QueueActor>();
        _fileService = fileService;

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
        Recover<DcJobAccepted>(dto => ApplyAccepted(dto.ToDomain()));
        Recover<DcStageEntered>(dto => ApplyStage(dto.ToDomain()));
        Recover<DcJobCompleted>(_ => _stage = "Done");
        Recover<DcJobFailed>(_ => _stage = "Failed");
        Recover<DcJobCancelled>(_ => _stage = "Cancelled");
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
        var evt = new DownloadActorStageEvents.JobAccepted(
            cmd.NzoId, cmd.VideoUrl, cmd.SubtitleUrl, cmd.Title, cmd.Category);

        Persist(evt.ToJournal(), _ =>
        {
            ApplyAccepted(evt);
            _log.Info("Job accepted for {NzoId} '{Title}'", _nzoId, _title);
            EnterFetching();
        });
    }

    private void EnterFetching()
    {
        var evt = new DownloadActorStageEvents.StageEntered(_nzoId, "Fetching");
        Persist(evt.ToJournal(), _ =>
        {
            ApplyStage(evt);
            NotifyTracker("Downloading");
            SpawnVideoWorker();
        });
    }

    private void Fetching()
    {
        Command<VideoFetched>(HandleVideoFetched);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleVideoFetched(VideoFetched msg)
    {
        _log.Debug("Video fetched for {NzoId}", msg.NzoId);
        EnterAcquiringSubtitle();
    }

    private void EnterAcquiringSubtitle()
    {
        var sourceType = DownloadSourceDetector.Detect(_videoUrl);
        var hlsManifestUrl = sourceType == DownloadSourceType.Hls ? _videoUrl : null;

        if (_subtitleUrl is null && hlsManifestUrl is null)
        {
            _hasSubtitle = false;
            EnterMuxing();
            return;
        }

        var evt = new DownloadActorStageEvents.StageEntered(_nzoId, "AcquiringSubtitle");
        Persist(evt.ToJournal(), _ =>
        {
            ApplyStage(evt);
            NotifyTracker("AcquiringSubtitle");
            SpawnSubtitleWorker(hlsManifestUrl);
        });
    }

    private void AcquiringSubtitle()
    {
        Command<SubtitleAcquired>(HandleSubtitleAcquired);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleSubtitleAcquired(SubtitleAcquired msg)
    {
        _hasSubtitle = msg.Found;
        if (_hasSubtitle)
        {
            EnterConvertingSubtitle();
        }
        else
        {
            EnterMuxing();
        }
    }

    private void EnterConvertingSubtitle()
    {
        var evt = new DownloadActorStageEvents.StageEntered(_nzoId, "ConvertingSubtitle");
        Persist(evt.ToJournal(), _ =>
        {
            ApplyStage(evt);
            SpawnSubtitleConvertActor();
        });
    }

    private void ConvertingSubtitle()
    {
        Command<SubtitleConverted>(HandleSubtitleConverted);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleSubtitleConverted(SubtitleConverted msg)
    {
        EnterMuxing();
    }

    private void EnterMuxing()
    {
        var evt = new DownloadActorStageEvents.StageEntered(_nzoId, "Muxing");
        Persist(evt.ToJournal(), _ =>
        {
            ApplyStage(evt);
            NotifyTracker("Muxing");
            SpawnRemuxActor();
        });
    }

    private void Muxing()
    {
        Command<VideoRemuxed>(HandleVideoRemuxed);
        Command<WorkerFailed>(HandleWorkerFailed);
        Command<CancelDownload>(HandleCancel);
        Command<Terminated>(HandleWorkerTerminated);
    }

    private void HandleVideoRemuxed(VideoRemuxed msg)
    {
        var outputPath = _fileService.GetOutputPath(_title, _category);
        var evt = new DownloadActorStageEvents.JobCompleted(_nzoId, outputPath);
        Persist(evt.ToJournal(), _ =>
        {
            _stage = "Done";
            _trackerShard.Tell(new DownloadRequestActor.CompleteDownload(_nzoId, outputPath));
            _queueActorRef.Tell(new QueueActor.NotifyJobFinished(_nzoId, "success"));
            _log.Info("Job completed for {NzoId}: {Path}", _nzoId, outputPath);
            Become(Completed);
        });
    }

    private void HandleWorkerFailed(WorkerFailed msg)
    {
        var evt = new DownloadActorStageEvents.JobFailed(_nzoId, msg.Kind.ToString(), msg.Reason);
        Persist(evt.ToJournal(), _ =>
        {
            _stage = "Failed";
            _trackerShard.Tell(new DownloadRequestActor.FailDownload(_nzoId, msg.Reason));
            _queueActorRef.Tell(new QueueActor.NotifyJobFinished(_nzoId, "failed"));
            _log.Warning("Job failed for {NzoId} ({Kind}): {Reason}", _nzoId, msg.Kind, msg.Reason);
            Become(Completed);
        });
    }

    private void HandleCancel(CancelDownload _)
    {
        _currentWorker?.Tell(PoisonPill.Instance);

        var evt = new DownloadActorStageEvents.JobCancelled(_nzoId);
        Persist(evt.ToJournal(), _ =>
        {
            _stage = "Cancelled";
            _trackerShard.Tell(new DownloadRequestActor.FailDownload(_nzoId, "Cancelled"));
            _queueActorRef.Tell(new QueueActor.NotifyJobFinished(_nzoId, "cancelled"));
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
        Command<VideoFetched>(_ => { });
        Command<SubtitleAcquired>(_ => { });
        Command<SubtitleConverted>(_ => { });
        Command<VideoRemuxed>(_ => { });
        Command<WorkerFailed>(_ => { });
        Command<Terminated>(_ => { });
    }

    private void SpawnVideoWorker()
    {
        var resolver = DependencyResolver.For(Context.System);
        var sourceType = DownloadSourceDetector.Detect(_videoUrl);

        if (sourceType == DownloadSourceType.Hls)
        {
            _currentWorker = Context.ActorOf(resolver.Props<HlsDownloadActor>(), "hls-video");
        }
        else
        {
            _currentWorker = Context.ActorOf(resolver.Props<Mp4DownloadActor>(), "mp4-video");
        }

        Context.Watch(_currentWorker);
        _currentWorker.Tell(new FetchVideo(_nzoId, _videoUrl));
        Become(Fetching);
    }

    private void SpawnSubtitleWorker(string? hlsManifestUrl)
    {
        var resolver = DependencyResolver.For(Context.System);
        var cmd = new AcquireSubtitle(_nzoId, _subtitleUrl, hlsManifestUrl);

        if (_subtitleUrl is not null)
        {
            _currentWorker = Context.ActorOf(resolver.Props<SubtitleDownloadActor>(), "subtitle-download");
        }
        else if (hlsManifestUrl is not null)
        {
            _currentWorker = Context.ActorOf(resolver.Props<SubtitleExtractActor>(), "subtitle-extract");
        }

        if (_currentWorker is not null)
        {
            Context.Watch(_currentWorker);
            _currentWorker.Tell(cmd);
        }

        Become(AcquiringSubtitle);
    }

    private void SpawnSubtitleConvertActor()
    {
        var resolver = DependencyResolver.For(Context.System);
        _currentWorker = Context.ActorOf(resolver.Props<SubtitleConvertActor>(), "subtitle-convert");
        Context.Watch(_currentWorker);
        _currentWorker.Tell(new ConvertSubtitle(_nzoId));
        Become(ConvertingSubtitle);
    }

    private void SpawnRemuxActor()
    {
        var resolver = DependencyResolver.For(Context.System);
        _currentWorker = Context.ActorOf(resolver.Props<RemuxActor>(), "remux");
        Context.Watch(_currentWorker);
        _currentWorker.Tell(new RemuxVideo(_nzoId, _title, _hasSubtitle, _category));
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
                SpawnSubtitleConvertActor();
                break;
            case "Muxing":
                SpawnRemuxActor();
                break;
            default:
                Become(WaitingForJob);
                break;
        }
    }

    private void NotifyTracker(string status)
    {
        _trackerShard.Tell(new DownloadRequestActor.ReportProgress(_nzoId, status));
    }

    private void ApplyAccepted(DownloadActorStageEvents.JobAccepted evt)
    {
        _nzoId = evt.NzoId;
        _videoUrl = evt.VideoUrl;
        _subtitleUrl = evt.SubtitleUrl;
        _title = evt.Title;
        _category = evt.Category;
        _stage = "Accepted";
    }

    private void ApplyStage(DownloadActorStageEvents.StageEntered evt)
    {
        _stage = evt.Stage;
    }
}
