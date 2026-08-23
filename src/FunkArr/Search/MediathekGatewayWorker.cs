using Akka.Actor;
using Akka.Event;

namespace FunkArr.Search;

internal sealed class MediathekGatewayWorker : ReceiveActor, IWithTimers
{
    private readonly MediathekClient _mediathekClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Queue<(FetchItems Request, IActorRef Sender)> _pending = new();
    private bool _inflight;

    private const string DrainTimerKey = "drain";
    private static readonly TimeSpan DrainInterval = TimeSpan.FromMilliseconds(500);

    public ITimerScheduler Timers { get; set; } = null!;

    public MediathekGatewayWorker(MediathekClient mediathekClient)
    {
        _mediathekClient = mediathekClient;

        ReceiveAsync<FetchItems>(HandleFetchAsync);
        Receive<DrainQueue>(_ => DrainNext());
    }

    private async Task HandleFetchAsync(FetchItems request)
    {
        if (_inflight)
        {
            _pending.Enqueue((request, Sender));
            EnsureDrainTimer();
            return;
        }

        await ExecuteRequestAsync(request, Sender);
    }

    private void DrainNext()
    {
        if (_inflight || _pending.Count == 0)
        {
            if (_pending.Count == 0)
            {
                Timers.Cancel(DrainTimerKey);
            }

            return;
        }

        var (request, sender) = _pending.Dequeue();
        ExecuteRequestAsync(request, sender).PipeTo(Self);
    }

    private async Task ExecuteRequestAsync(FetchItems request, IActorRef sender)
    {
        _inflight = true;
        try
        {
            var items = await SearchChildHelpers.SearchMediathekAsync(
                _mediathekClient, _log, request.SearchTerm);
            sender.Tell(new ItemsFetched(items));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Mediathek fetch failed for '{SearchTerm}'", request.SearchTerm);
            sender.Tell(new ItemsFetched(Array.Empty<MediathekResultItem>()));
        }
        finally
        {
            _inflight = false;
        }
    }

    private void EnsureDrainTimer()
    {
        if (!Timers.IsTimerActive(DrainTimerKey))
        {
            Timers.StartPeriodicTimer(DrainTimerKey, DrainQueue.Instance, DrainInterval, DrainInterval);
        }
    }

    private sealed class DrainQueue
    {
        public static readonly DrainQueue Instance = new();
        private DrainQueue() { }
    }
}
