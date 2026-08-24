using Akka.Actor;
using Akka.Event;

namespace FunkArr.Search;

public sealed class MediathekGatewayActor : ReceiveActor, IWithTimers
{
    private readonly MediathekClient _mediathekClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Queue<(FetchItems Request, IActorRef Sender)> _pending = new();
    private bool _inflight;

    private const string DrainTimerKey = "drain";
    private static readonly TimeSpan DrainInterval = TimeSpan.FromMilliseconds(500);

    public ITimerScheduler Timers { get; set; } = null!;

    public MediathekGatewayActor(MediathekClient mediathekClient)
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
            var searchTerm = request.SearchTerm;
            var isBlank = string.IsNullOrWhiteSpace(searchTerm);
            var query = new MediathekQuery
            {
                Queries = isBlank
                    ? []
                    : [new MediathekQueryItem { Fields = ["topic", "title"], Query = searchTerm }],
                Size = isBlank ? 100 : 5000,
            };

            var response = await _mediathekClient.QueryAsync(query);
            var items = response?.Result?.Results ?? [];
            _log.Info("MediathekViewWeb query for '{SearchTerm}' returned {Count} items", searchTerm, items.Length);
            sender.Tell(new ItemsFetched(items));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Mediathek fetch failed for '{SearchTerm}'", request.SearchTerm);
            sender.Tell(new ItemsFetched([]));
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
