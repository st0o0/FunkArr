using System.Diagnostics.Metrics;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence;
using FunkArr.Configuration;
using FunkArr.Diagnostics;
using FunkArr.DownloadClient.Pipeline;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Persistence;
using Microsoft.Extensions.Options;

namespace FunkArr.DownloadClient.Queue;

public sealed class QueueActor : ReceivePersistentActor, IWithStash
{
    public override string PersistenceId => "queue-coordinator";
    public new IStash Stash { get; set; } = null!;

    private readonly int _maxConcurrent;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Gauge<double> _queueDepth = FunkArrMetrics.Instance.AddQueueDepth();

    private readonly LinkedList<QueueEntry> _queue = new();
    private readonly HashSet<string> _active = [];
    private readonly Dictionary<string, QueueEntry> _allJobs = new();
    private readonly List<string> _completedJobIds = [];

    private readonly IActorRef _trackerShard;
    private readonly IActorRef _coordinatorShard;

    public sealed record Enqueue(string DownloadUrl, string Title, string? SubtitleUrl, string? Category);
    public sealed record Cancel(string NzoId);
    public sealed record NotifyJobFinished(string NzoId, string Outcome);
    public sealed record GetQueueOrder;
    public sealed record QueueOrderResponse(IReadOnlyList<QueueOrderEntry> Entries);
    public sealed record QueueOrderEntry(string NzoId, string Title, string Status, string? Category);
    public sealed record GetCompletedJobIds;
    public sealed record CompletedJobIdsResponse(IReadOnlyList<string> NzoIds);

    internal sealed record QueueEntry(
        string NzoId, string DownloadUrl, string Title, string? SubtitleUrl, string? Category, DateTimeOffset EnqueuedAt);

    public QueueActor(IOptions<DownloadOptions> options, IActorRegistry actorRegistry)
    {
        _maxConcurrent = options.Value.ConcurrentDownloads;
        _trackerShard = actorRegistry.Get<DownloadRequestActor>();
        _coordinatorShard = actorRegistry.Get<DownloadActor>();
        Recovering();
    }

    private void Recovering()
    {
        Recover<QueueJobEnqueued>(j => Apply(j.ToDomain()));
        Recover<QueueJobStarted>(j => Apply(j.ToDomain()));
        Recover<QueueJobFinished>(j => Apply(j.ToDomain()));
        Recover<QueueJobRemoved>(j => Apply(j.ToDomain()));
        Recover<RecoveryCompleted>(_ => OnRecoveryCompleted());

        CommandAny(msg =>
        {
            if (msg is not RecoveryCompleted)
            {
                Stash.Stash();
            }
        });
    }

    private void OnRecoveryCompleted()
    {
        foreach (var nzoId in _active.ToList())
        {
            _active.Remove(nzoId);
            if (_allJobs.TryGetValue(nzoId, out var entry))
            {
                _queue.AddFirst(entry);
            }
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
        var evt = new QueueActorEvents.JobEnqueued(
            nzoId, cmd.DownloadUrl, cmd.Title, cmd.SubtitleUrl, cmd.Category, DateTimeOffset.UtcNow);

        Persist(evt.ToJournal(), _ =>
        {
            Apply(evt);
            RecordQueueDepth();

            _trackerShard.Tell(new DownloadRequestActor.TrackDownload(
                nzoId, cmd.Title, cmd.DownloadUrl, cmd.Category, DateTimeOffset.UtcNow));

            TryStartNext();
            Sender.Tell(nzoId);
        });
    }

    private void HandleCancel(Cancel cmd)
    {
        var node = FindInQueue(cmd.NzoId);
        if (node is not null)
        {
            var evt = new QueueActorEvents.JobRemoved(cmd.NzoId);
            Persist(evt.ToJournal(), _ =>
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

        var evt = new QueueActorEvents.JobFinished(msg.NzoId, msg.Outcome);
        Persist(evt.ToJournal(), _ =>
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
            {
                entries.Add(new QueueOrderEntry(nzoId, entry.Title, "active", entry.Category));
            }
        }

        foreach (var entry in _queue)
        {
            entries.Add(new QueueOrderEntry(entry.NzoId, entry.Title, "queued", entry.Category));
        }

        Sender.Tell(new QueueOrderResponse(entries));
    }

    private void HandleGetCompletedJobIds(GetCompletedJobIds _)
    {
        Sender.Tell(new CompletedJobIdsResponse(_completedJobIds.ToList()));
    }

    private void TryStartNext()
    {
        var toStart = new List<(QueueEntry Entry, QueueActorEvents.JobStarted Event)>();

        while (_active.Count + toStart.Count < _maxConcurrent && _queue.Count > 0)
        {
            var entry = _queue.First!.Value;
            _queue.RemoveFirst();
            toStart.Add((entry, new QueueActorEvents.JobStarted(entry.NzoId)));
        }

        if (toStart.Count == 0)
        {
            return;
        }

        var dtos = toStart.Select(x => (object)x.Event.ToJournal()).ToArray();
        var idx = 0;
        PersistAll(dtos, _ =>
        {
            var (entry, evt) = toStart[idx++];
            Apply(evt);

            _coordinatorShard.Tell(new StartDownload(
                entry.NzoId, entry.DownloadUrl, entry.SubtitleUrl, entry.Title, entry.Category));

            _log.Info("Started job {NzoId} '{Title}'. Active: {Active}, Queued: {Queued}",
                entry.NzoId, entry.Title, _active.Count, _queue.Count);
        });
    }

    private void Apply(QueueActorEvents.JobEnqueued evt)
    {
        var entry = new QueueEntry(evt.NzoId, evt.DownloadUrl, evt.Title, evt.SubtitleUrl, evt.Category, evt.EnqueuedAt);
        _allJobs[evt.NzoId] = entry;
        _queue.AddLast(entry);
    }

    private void Apply(QueueActorEvents.JobStarted evt)
    {
        var node = FindInQueue(evt.NzoId);
        if (node is not null)
        {
            _queue.Remove(node);
        }

        _active.Add(evt.NzoId);
    }

    private void Apply(QueueActorEvents.JobFinished evt)
    {
        _active.Remove(evt.NzoId);
        _allJobs.Remove(evt.NzoId);
        _completedJobIds.Add(evt.NzoId);
    }

    private void Apply(QueueActorEvents.JobRemoved evt)
    {
        var node = FindInQueue(evt.NzoId);
        if (node is not null)
        {
            _queue.Remove(node);
        }

        _active.Remove(evt.NzoId);
        _allJobs.Remove(evt.NzoId);
    }

    private LinkedListNode<QueueEntry>? FindInQueue(string nzoId)
    {
        var node = _queue.First;
        while (node is not null)
        {
            if (node.Value.NzoId == nzoId)
            {
                return node;
            }

            node = node.Next;
        }

        return null;
    }

    private void RecordQueueDepth()
    {
        _queueDepth.Record(_active.Count + _queue.Count);
    }
}
