using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Supervision;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

public sealed class MediathekViewWebManager : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Dictionary<Guid, PendingQuery> _pending = [];
    private readonly TimeProvider _timeProvider;
    private ISourceQueueWithComplete<StreamRequest> _queue = null!;

    private sealed record PendingQuery(IActorRef Sender, MediathekQueryPlan Plan);

    private sealed record StreamRequest(string Json, Guid RequestId);

    private abstract record StreamResponse(Guid RequestId);

    private sealed record StreamSuccess(MediathekQueryResult Result, Guid RequestId) : StreamResponse(RequestId);

    private sealed record StreamFailure(Exception Cause, Guid RequestId) : StreamResponse(RequestId);

    private sealed record StreamComplete
    {
        public static readonly StreamComplete Instance = new();
    }

    private sealed record OfferOutcome(Guid RequestId, IQueueOfferResult? Result, Exception? Error);

    public MediathekViewWebManager(MediathekClient client, TimeProvider timeProvider, int maxConcurrent = 3)
    {
        _timeProvider = timeProvider;
        var materializer = Context.Materializer();
        _queue = Source.Queue<StreamRequest>(64, OverflowStrategy.DropNew)
            .SelectAsyncUnordered(maxConcurrent, async tuple =>
            {
                try
                {
                    var result = await client.QueryAsync(tuple.Json);
                    return (StreamResponse)new StreamSuccess(result, tuple.RequestId);
                }
                catch (Exception ex)
                {
                    return new StreamFailure(ex, tuple.RequestId);
                }
            })
            .WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))
            .To(Sink.ActorRef<StreamResponse>(Self, StreamComplete.Instance, ex => new StreamFailure(ex, Guid.Empty)))
            .Run(materializer);

        Receive<QueryMediathek>(query =>
        {
            var requestId = Guid.NewGuid();
            var plan = MediathekQueryPlan.For(query, _timeProvider.GetUtcNow());
            _pending[requestId] = new PendingQuery(Sender, plan);
            Offer(requestId, plan.FirstPage());
        });

        Receive<OfferOutcome>(msg =>
        {
            if (msg.Result is QueueOfferResult.Enqueued)
            {
                return;
            }

            if (!_pending.Remove(msg.RequestId, out var pending))
            {
                return;
            }

            if (msg.Error is not null)
            {
                pending.Sender.Tell(new QueryMediathekError(msg.Error));
            }
            else
            {
                pending.Sender.Tell(new QueryMediathekQueueFull());
            }
        });

        Receive<StreamSuccess>(msg =>
        {
            if (!_pending.TryGetValue(msg.RequestId, out var pending))
            {
                return;
            }

            var nextPage = pending.Plan.Apply(msg.Result);
            if (nextPage is not null)
            {
                Offer(msg.RequestId, nextPage);
                return;
            }

            _pending.Remove(msg.RequestId);
            pending.Sender.Tell(pending.Plan.ToCompleted());
        });

        Receive<StreamFailure>(msg =>
        {
            Telemetry.MediathekErrors.Add(1);
            _log.Warning(msg.Cause, "MediathekViewWeb query {RequestId} failed", msg.RequestId);
            if (_pending.Remove(msg.RequestId, out var pending))
            {
                pending.Sender.Tell(new QueryMediathekError(msg.Cause));
            }
        });

        Receive<StreamComplete>(_ =>
            throw new InvalidOperationException(
                $"MediathekViewWeb stream completed unexpectedly, {_pending.Count} requests orphaned"));
    }

    private void Offer(Guid requestId, string json) =>
        _queue.OfferAsync(new StreamRequest(json, requestId))
            .PipeTo(Self,
                success: result => new OfferOutcome(requestId, result, null),
                failure: ex => new OfferOutcome(requestId, null, ex));
}
