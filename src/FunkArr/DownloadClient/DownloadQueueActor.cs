using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Streams;
using Akka.Streams.Dsl;
using FunkArr.Configuration;
using FunkArr.Muxing;
using FunkArr.Persistence;
using FunkArr.Shared;
using Microsoft.Extensions.Options;

namespace FunkArr.DownloadClient;

public sealed class DownloadQueueActor : ReceivePersistentActor, IWithStash
{
    public override string PersistenceId => "download-queue";
    public new IStash Stash { get; set; } = null!;

    private readonly DownloadService _downloadService;
    private readonly MuxingService _muxingService;
    private readonly IFileService _fileService;
    private readonly DownloadOptions _options;
    private readonly Dictionary<string, DownloadJob> _jobs = new();
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private IMaterializer? _materializer;
    private ISourceQueueWithComplete<DownloadRequest>? _queue;
    private SharedKillSwitch? _killSwitch;

    public sealed record EnqueueDownload(string DownloadUrl, string Title, string? SubtitleUrl);

    public sealed record GetQueue;

    public sealed record GetHistory;

    public sealed record QueueResponse(IReadOnlyList<DownloadJob> Jobs);

    public sealed record HistoryResponse(IReadOnlyList<DownloadJob> Jobs);

    private sealed record StreamCompleted;

    private sealed record StreamFailed(Exception Reason);

    private sealed record OfferResult(string NzoId, IQueueOfferResult Result);

    public DownloadQueueActor(
        DownloadService downloadService,
        MuxingService muxingService,
        IFileService fileService,
        IOptions<DownloadOptions> options)
    {
        _downloadService = downloadService;
        _muxingService = muxingService;
        _fileService = fileService;
        _options = options.Value;

        Recovering();
    }

    protected override void PreStart()
    {
        _fileService.EnsureDirectoriesExist(_options.TempPath, _options.DownloadPath);
    }

    protected override void PostStop()
    {
        _killSwitch?.Shutdown();
        _queue?.Complete();
    }

    private void Recovering()
    {
        Recover<DownloadEnqueuedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)));
        Recover<DownloadStartedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)));
        Recover<DownloadCompletedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)));
        Recover<DownloadFailedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)));
        Recover<MuxingStartedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)));
        Recover<MuxingCompletedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)));
        Recover<MuxingFailedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)));
        Recover<RecoveryCompleted>(_ => OnRecoveryCompleted());

        CommandAny(msg =>
        {
            if (msg is not RecoveryCompleted)
            {
                Stash.Stash();
            }
        });
    }

    private void Materializing()
    {
        _materializer = Context.Materializer();
        MaterializeStream();
        Become(Ready);
        Stash.UnstashAll();
    }

    private void Ready()
    {
        Command<EnqueueDownload>(HandleEnqueue);
        Command<GetQueue>(HandleGetQueue);
        Command<GetHistory>(HandleGetHistory);
        Command<DownloadEvents.DownloadProgressUpdated>(HandleProgressUpdate);
        Command<DownloadEvents.DownloadStarted>(HandleDownloadStarted);
        Command<DownloadEvents.DownloadCompleted>(HandleDownloadCompleted);
        Command<DownloadEvents.DownloadFailed>(HandleDownloadFailed);
        Command<DownloadEvents.MuxingStarted>(HandleMuxingStarted);
        Command<DownloadEvents.MuxingCompleted>(HandleMuxingCompleted);
        Command<DownloadEvents.MuxingFailed>(HandleMuxingFailed);
        Command<StreamCompleted>(_ => HandleStreamCompleted());
        Command<StreamFailed>(msg => HandleStreamFailed(msg.Reason));
        Command<OfferResult>(HandleOfferResult);
    }

    private void MaterializeStream()
    {
        _killSwitch?.Shutdown();
        _killSwitch = KillSwitches.Shared("download-pipeline");
        var self = Self;

        var (queue, done) = Source.Queue<DownloadRequest>(64, OverflowStrategy.Backpressure)
            .Via(_killSwitch.Flow<DownloadRequest>())
            .SelectAsyncUnordered(_options.ConcurrentDownloads, async req =>
            {
                try
                {
                    self.Tell(new DownloadEvents.DownloadStarted(req.NzoId));
                    var (videoPath, subtitlePath) = await _downloadService.DownloadAsync(
                        req,
                        onProgress: (downloaded, total) =>
                            self.Tell(new DownloadEvents.DownloadProgressUpdated(req.NzoId, downloaded, total)),
                        cancellationToken: default);
                    self.Tell(new DownloadEvents.DownloadCompleted(req.NzoId, videoPath, subtitlePath));
                    return (DownloadOutcome)new DownloadOutcome.Success(req.NzoId, videoPath, subtitlePath);
                }
                catch (Exception ex)
                {
                    self.Tell(new DownloadEvents.DownloadFailed(req.NzoId, ex.Message));
                    return new DownloadOutcome.Failure(req.NzoId, ex.Message);
                }
            })
            .SelectAsyncUnordered(_options.ConcurrentDownloads, async outcome =>
            {
                switch (outcome)
                {
                    case DownloadOutcome.Success s:
                        self.Tell(new DownloadEvents.MuxingStarted(s.NzoId));
                        var result = await _muxingService.MuxAsync(
                            s.VideoPath, s.SubtitlePath,
                            _options.DownloadPath,
                            _jobs.TryGetValue(s.NzoId, out var job) ? job.Title : s.NzoId);
                        return result;

                    case DownloadOutcome.Failure f:
                        return new MuxOutcome.Skipped(f.NzoId, f.Reason);

                    default:
                        return new MuxOutcome.Skipped(outcome.NzoId, "Unknown outcome");
                }
            })
            .ToMaterialized(Sink.ForEach<MuxOutcome>(muxResult =>
            {
                switch (muxResult)
                {
                    case MuxOutcome.Success s:
                        self.Tell(new DownloadEvents.MuxingCompleted(s.NzoId, s.OutputPath));
                        break;
                    case MuxOutcome.Failure f:
                        self.Tell(new DownloadEvents.MuxingFailed(f.NzoId, f.Reason));
                        break;
                    case MuxOutcome.Skipped:
                        break;
                }
            }), Keep.Both)
            .WithAttributes(ActorAttributes.CreateSupervisionStrategy(
                StreamSupervision.LoggingDecider(_log)))
            .Run(_materializer);

        _queue = queue;
        done.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                return (object)new StreamFailed(t.Exception!.GetBaseException());
            }

            return new StreamCompleted();
        }).PipeTo(self);
    }

    private void HandleEnqueue(EnqueueDownload cmd)
    {
        var nzoId = Guid.NewGuid().ToString("N")[..10];
        var evt = new DownloadEvents.DownloadEnqueued(
            nzoId, cmd.DownloadUrl, cmd.Title, cmd.SubtitleUrl, DateTimeOffset.UtcNow);

        Persist(DownloadEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyEvent(evt);
            OfferToQueue(nzoId);
            Sender.Tell(nzoId);
        });
    }

    private void OfferToQueue(string nzoId)
    {
        if (_queue is null || !_jobs.TryGetValue(nzoId, out var job) || job.Status != DownloadStatus.Queued)
        {
            return;
        }

        var request = new DownloadRequest(
            job.NzoId, job.DownloadUrl, job.SubtitleUrl,
            _options.TempPath, _options.DownloadPath, job.Title);

        _queue.OfferAsync(request).PipeTo(Self,
            success: result => new OfferResult(nzoId, result),
            failure: ex => new OfferResult(nzoId, new QueueOfferResult.Failure(ex)));
    }

    private void HandleOfferResult(OfferResult msg)
    {
        if (msg.Result is QueueOfferResult.Failure f)
        {
            _log.Warning("Failed to offer {NzoId} to stream: {Error}", msg.NzoId, f.Cause.Message);
        }
        else if (msg.Result is QueueOfferResult.Dropped)
        {
            _log.Warning("Offer for {NzoId} was dropped — queue full", msg.NzoId);
        }
    }

    private void HandleGetQueue(GetQueue _)
    {
        var active = _jobs.Values
            .Where(j => j.Status is DownloadStatus.Queued or DownloadStatus.Downloading or DownloadStatus.Muxing)
            .ToList();
        Sender.Tell(new QueueResponse(active));
    }

    private void HandleGetHistory(GetHistory _)
    {
        var history = _jobs.Values
            .Where(j => j.Status is DownloadStatus.Completed or DownloadStatus.Failed)
            .OrderByDescending(j => j.CompletedAt)
            .ToList();
        Sender.Tell(new HistoryResponse(history));
    }

    private void HandleDownloadStarted(DownloadEvents.DownloadStarted evt)
    {
        Persist(DownloadEventDtoMapping.ToDto(evt), _ => ApplyEvent(evt));
    }

    private void HandleProgressUpdate(DownloadEvents.DownloadProgressUpdated evt)
    {
        if (_jobs.TryGetValue(evt.NzoId, out var job))
        {
            var progress = evt.TotalBytes > 0 ? (double)evt.DownloadedBytes / evt.TotalBytes * 100 : 0;
            _jobs[evt.NzoId] = job with
            {
                DownloadedBytes = evt.DownloadedBytes,
                TotalBytes = evt.TotalBytes,
                ProgressPercent = progress,
            };
        }
    }

    private void HandleDownloadCompleted(DownloadEvents.DownloadCompleted evt)
    {
        Persist(DownloadEventDtoMapping.ToDto(evt), _ => ApplyEvent(evt));
    }

    private void HandleDownloadFailed(DownloadEvents.DownloadFailed evt)
    {
        Persist(DownloadEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyEvent(evt);
            _log.Warning("Download failed for {NzoId}: {Error}", evt.NzoId, evt.Error);
        });
    }

    private void HandleMuxingStarted(DownloadEvents.MuxingStarted evt)
    {
        Persist(DownloadEventDtoMapping.ToDto(evt), _ => ApplyEvent(evt));
    }

    private void HandleMuxingCompleted(DownloadEvents.MuxingCompleted evt)
    {
        Persist(DownloadEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyEvent(evt);
            _log.Info("Download and muxing complete for {NzoId}: {Path}", evt.NzoId, evt.OutputPath);
        });
    }

    private void HandleMuxingFailed(DownloadEvents.MuxingFailed evt)
    {
        Persist(DownloadEventDtoMapping.ToDto(evt), _ =>
        {
            ApplyEvent(evt);
            _log.Warning("Muxing failed for {NzoId}: {Error}", evt.NzoId, evt.Error);
        });
    }

    private void HandleStreamCompleted()
    {
        _log.Info("Download stream completed — re-materializing");
        MaterializeStream();
        PushQueuedJobs();
    }

    private void HandleStreamFailed(Exception reason)
    {
        _log.Warning(reason, "Download stream failed — re-materializing");
        MaterializeStream();
        ResetInFlightJobs();
        PushQueuedJobs();
    }

    private void OnRecoveryCompleted()
    {
        ResetInFlightJobs();
        _log.Info("Recovery completed. {QueuedCount} jobs in queue",
            _jobs.Values.Count(j => j.Status == DownloadStatus.Queued));
        Materializing();
    }

    private void ResetInFlightJobs()
    {
        foreach (var job in _jobs.Values)
        {
            if (job.Status is DownloadStatus.Downloading or DownloadStatus.Muxing)
            {
                _jobs[job.NzoId] = job with { Status = DownloadStatus.Queued };
            }
        }
    }

    private void PushQueuedJobs()
    {
        foreach (var job in _jobs.Values.Where(j => j.Status == DownloadStatus.Queued))
            OfferToQueue(job.NzoId);
    }

    private void ApplyEvent(DownloadEvents.DownloadEnqueued evt)
    {
        _jobs[evt.NzoId] = new DownloadJob
        {
            NzoId = evt.NzoId,
            DownloadUrl = evt.DownloadUrl,
            Title = evt.Title,
            SubtitleUrl = evt.SubtitleUrl,
            EnqueuedAt = evt.EnqueuedAt,
        };
    }

    private void ApplyEvent(DownloadEvents.DownloadStarted evt)
    {
        if (_jobs.TryGetValue(evt.NzoId, out var job))
        {
            _jobs[evt.NzoId] = job with { Status = DownloadStatus.Downloading };
        }
    }

    private void ApplyEvent(DownloadEvents.DownloadCompleted evt)
    {
        if (_jobs.TryGetValue(evt.NzoId, out var job))
        {
            _jobs[evt.NzoId] = job with { Status = DownloadStatus.Muxing };
        }
    }

    private void ApplyEvent(DownloadEvents.DownloadFailed evt)
    {
        if (_jobs.TryGetValue(evt.NzoId, out var job))
        {
            _jobs[evt.NzoId] = job with
            {
                Status = DownloadStatus.Failed,
                ErrorMessage = evt.Error,
                CompletedAt = DateTimeOffset.UtcNow,
            };
        }
    }

    private void ApplyEvent(DownloadEvents.MuxingStarted evt)
    {
        if (_jobs.TryGetValue(evt.NzoId, out var job))
        {
            _jobs[evt.NzoId] = job with { Status = DownloadStatus.Muxing };
        }
    }

    private void ApplyEvent(DownloadEvents.MuxingCompleted evt)
    {
        if (_jobs.TryGetValue(evt.NzoId, out var job))
        {
            _jobs[evt.NzoId] = job with
            {
                Status = DownloadStatus.Completed,
                OutputPath = evt.OutputPath,
                CompletedAt = DateTimeOffset.UtcNow,
            };
        }
    }

    private void ApplyEvent(DownloadEvents.MuxingFailed evt)
    {
        if (_jobs.TryGetValue(evt.NzoId, out var job))
        {
            _jobs[evt.NzoId] = job with
            {
                Status = DownloadStatus.Failed,
                ErrorMessage = evt.Error,
                CompletedAt = DateTimeOffset.UtcNow,
            };
        }
    }
}