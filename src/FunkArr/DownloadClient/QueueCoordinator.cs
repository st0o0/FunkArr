using System.Diagnostics.Metrics;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence;
using FunkArr.Configuration;
using FunkArr.Diagnostics;
using FunkArr.Persistence;
using Microsoft.Extensions.Options;

namespace FunkArr.DownloadClient;

public sealed class QueueCoordinator : ReceivePersistentActor, IWithStash
{
    public override string PersistenceId => "queue-coordinator";
    public new IStash Stash { get; set; } = null!;

    private readonly int _maxConcurrent;
    private readonly string _tempPath;
    private readonly string _downloadPath;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Gauge<double> _queueDepth = FunkArrMetrics.Instance.AddQueueDepth();

    private readonly LinkedList<QueueEntry> _queue = new();
    private readonly HashSet<string> _active = [];
    private readonly Dictionary<string, QueueEntry> _allJobs = new();
    private readonly List<string> _completedJobIds = [];

    private readonly IActorRef _trackerShard;
    private readonly IActorRef _coordinatorShard;

    public sealed record Enqueue(string DownloadUrl, string Title, string? SubtitleUrl);
    public sealed record Cancel(string NzoId);
    public sealed record NotifyJobFinished(string NzoId, string Outcome);
    public sealed record GetQueueOrder;
    public sealed record QueueOrderResponse(IReadOnlyList<QueueOrderEntry> Entries);
    public sealed record QueueOrderEntry(string NzoId, string Title, string Status);
    public sealed record GetCompletedJobIds;
    public sealed record CompletedJobIdsResponse(IReadOnlyList<string> NzoIds);

    internal sealed record QueueEntry(
        string NzoId, string DownloadUrl, string Title, string? SubtitleUrl, DateTimeOffset EnqueuedAt);

    public QueueCoordinator(IOptions<DownloadOptions> options, IActorRegistry actorRegistry)
    {
        var opts = options.Value;
        _maxConcurrent = opts.ConcurrentDownloads;
        _tempPath = opts.TempPath;
        _downloadPath = opts.DownloadPath ?? string.Empty;
        _trackerShard = actorRegistry.Get<DownloadRequestTracker>();
        _coordinatorShard = actorRegistry.Get<DownloadCoordinator>();
        Recovering();
    }

    private void Recovering()
    {
        Recover<QueueJobEnqueuedDto>(dto => Apply(QueueCoordinatorEventDtoMapping.ToDomain(dto)));
        Recover<QueueJobStartedDto>(dto => Apply(QueueCoordinatorEventDtoMapping.ToDomain(dto)));
        Recover<QueueJobFinishedDto>(dto => Apply(QueueCoordinatorEventDtoMapping.ToDomain(dto)));
        Recover<QueueJobRemovedDto>(dto => Apply(QueueCoordinatorEventDtoMapping.ToDomain(dto)));
        Recover<RecoveryCompleted>(_ => OnRecoveryCompleted());

        CommandAny(msg =>
        {
            if (msg is not RecoveryCompleted)
                Stash.Stash();
        });
    }

    private void OnRecoveryCompleted()
    {
        foreach (var nzoId in _active.ToList())
        {
            _active.Remove(nzoId);
            if (_allJobs.TryGetValue(nzoId, out var entry))
                _queue.AddFirst(entry);
        }

        _log.Info("Recovery completed. {QueuedCount} queued", _queue.Count);

        Become(Ready);
        Stash.UnstashAll();
        TryStartNext();
    }

    private void Ready()
    {
        Command<Enqueue>(HandleEnqueue);
        Command<Cancel>(HandleCancel);
        Command<NotifyJobFinished>(HandleJobFinished);
        Command<GetQueueOrder>(HandleGetQueueOrder);
        Command<GetCompletedJobIds>(HandleGetCompletedJobIds);
    }

    private void HandleEnqueue(Enqueue cmd)
    {
        var nzoId = Guid.NewGuid().ToString("N")[..10];
        var evt = new QueueCoordinatorEvents.JobEnqueued(
            nzoId, cmd.DownloadUrl, cmd.Title, cmd.SubtitleUrl, DateTimeOffset.UtcNow);

        Persist(QueueCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
            RecordQueueDepth();

            _trackerShard.Tell(new DownloadRequestTracker.CreateRequest(
                nzoId, cmd.Title, cmd.DownloadUrl, DateTimeOffset.UtcNow));

            TryStartNext();
            Sender.Tell(nzoId);
        });
    }

    private void HandleCancel(Cancel cmd)
    {
        var node = FindInQueue(cmd.NzoId);
        if (node is not null)
        {
            var evt = new QueueCoordinatorEvents.JobRemoved(cmd.NzoId);
            Persist(QueueCoordinatorEventDtoMapping.ToDto(evt), _ =>
            {
                Apply(evt);
                RecordQueueDepth();
                _log.Info("Cancelled queued job {NzoId}", cmd.NzoId);
            });
            return;
        }

        if (_active.Contains(cmd.NzoId))
        {
            _coordinatorShard.Tell(new CancelDownload(cmd.NzoId));
            _log.Info("Sent cancel to active download {NzoId}", cmd.NzoId);
            return;
        }

        _log.Debug("Cancel for unknown job {NzoId}, ignoring", cmd.NzoId);
    }

    private void HandleJobFinished(NotifyJobFinished msg)
    {
        if (!_active.Remove(msg.NzoId))
        {
            _log.Debug("JobFinished for {NzoId} which is not in active set", msg.NzoId);
            return;
        }

        var evt = new QueueCoordinatorEvents.JobFinished(msg.NzoId, msg.Outcome);
        Persist(QueueCoordinatorEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
            RecordQueueDepth();
            TryStartNext();
            _log.Info("Job {NzoId} finished with outcome {Outcome}. Active: {Active}, Queued: {Queued}",
                msg.NzoId, msg.Outcome, _active.Count, _queue.Count);
        });
    }

    private void HandleGetQueueOrder(GetQueueOrder _)
    {
        var entries = new List<QueueOrderEntry>();

        foreach (var nzoId in _active)
        {
            if (_allJobs.TryGetValue(nzoId, out var entry))
                entries.Add(new QueueOrderEntry(nzoId, entry.Title, "active"));
        }

        foreach (var entry in _queue)
        {
            entries.Add(new QueueOrderEntry(entry.NzoId, entry.Title, "queued"));
        }

        Sender.Tell(new QueueOrderResponse(entries));
    }

    private void HandleGetCompletedJobIds(GetCompletedJobIds _)
    {
        Sender.Tell(new CompletedJobIdsResponse(_completedJobIds.ToList()));
    }

    private void TryStartNext()
    {
        var toStart = new List<(QueueEntry Entry, QueueCoordinatorEvents.JobStarted Event)>();

        while (_active.Count + toStart.Count < _maxConcurrent && _queue.Count > 0)
        {
            var entry = _queue.First!.Value;
            _queue.RemoveFirst();
            toStart.Add((entry, new QueueCoordinatorEvents.JobStarted(entry.NzoId)));
        }

        if (toStart.Count == 0) return;

        var dtos = toStart.Select(x => (object)QueueCoordinatorEventDtoMapping.ToDto(x.Event)).ToArray();
        var idx = 0;
        PersistAll(dtos, _ =>
        {
            var (entry, evt) = toStart[idx++];
            Apply(evt);

            _coordinatorShard.Tell(new StartDownload(
                entry.NzoId, entry.DownloadUrl, entry.SubtitleUrl,
                _tempPath, _downloadPath, entry.Title));

            _log.Info("Started job {NzoId} '{Title}'. Active: {Active}, Queued: {Queued}",
                entry.NzoId, entry.Title, _active.Count, _queue.Count);
        });
    }

    private void Apply(QueueCoordinatorEvents.JobEnqueued evt)
    {
        var entry = new QueueEntry(evt.NzoId, evt.DownloadUrl, evt.Title, evt.SubtitleUrl, evt.EnqueuedAt);
        _allJobs[evt.NzoId] = entry;
        _queue.AddLast(entry);
    }

    private void Apply(QueueCoordinatorEvents.JobStarted evt)
    {
        var node = FindInQueue(evt.NzoId);
        if (node is not null)
            _queue.Remove(node);
        _active.Add(evt.NzoId);
    }

    private void Apply(QueueCoordinatorEvents.JobFinished evt)
    {
        _active.Remove(evt.NzoId);
        _allJobs.Remove(evt.NzoId);
        _completedJobIds.Add(evt.NzoId);
    }

    private void Apply(QueueCoordinatorEvents.JobRemoved evt)
    {
        var node = FindInQueue(evt.NzoId);
        if (node is not null)
            _queue.Remove(node);
        _active.Remove(evt.NzoId);
        _allJobs.Remove(evt.NzoId);
    }

    private LinkedListNode<QueueEntry>? FindInQueue(string nzoId)
    {
        var node = _queue.First;
        while (node is not null)
        {
            if (node.Value.NzoId == nzoId)
                return node;
            node = node.Next;
        }

        return null;
    }

    private void RecordQueueDepth()
    {
        _queueDepth.Record(_active.Count + _queue.Count);
    }
}
