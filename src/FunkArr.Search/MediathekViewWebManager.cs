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
    private readonly Dictionary<Guid, IActorRef> _pending = [];

    private sealed record StreamRequest(string Json, Guid RequestId);

    private abstract record StreamResponse(Guid RequestId);

    private sealed record StreamSuccess(MediathekQueryResult Result, Guid RequestId) : StreamResponse(RequestId);

    private sealed record StreamFailure(Exception Cause, Guid RequestId) : StreamResponse(RequestId);

    private sealed record StreamComplete
    {
        public static readonly StreamComplete Instance = new();
    }

    public MediathekViewWebManager(MediathekClient client, int maxConcurrent = 3)
    {
        var materializer = Context.Materializer();

        var sourceRef = Source.ActorRef<StreamRequest>(64, OverflowStrategy.Backpressure)
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
            _pending[requestId] = Sender;
            sourceRef.Tell(new StreamRequest(MediathekQueryBuilder.FromMessage(query).Build(), requestId));
        });

        Receive<StreamSuccess>(msg =>
        {
            if (_pending.Remove(msg.RequestId, out var sender))
            {
                sender.Tell(new QueryMediathekCompleted(msg.Result.Items, msg.Result.Total));
            }
        });

        Receive<StreamFailure>(msg =>
        {
            Telemetry.MediathekErrors.Add(1);
            _log.Warning(msg.Cause, "MediathekViewWeb query {RequestId} failed", msg.RequestId);
            if (_pending.Remove(msg.RequestId, out var sender))
            {
                sender.Tell(new QueryMediathekFailed(msg.Cause));
            }
        });

        Receive<StreamComplete>(_ =>
            throw new InvalidOperationException(
                $"MediathekViewWeb stream completed unexpectedly, {_pending.Count} requests orphaned"));
    }
}
