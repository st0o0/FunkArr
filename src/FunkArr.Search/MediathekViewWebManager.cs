using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

public sealed class MediathekViewWebManager : ReceiveActor, IWithUnboundedStash
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly MediathekClient _client;
    private readonly int _maxConcurrent;
    private MediathekViewWebManagerState _state = MediathekViewWebManagerState.Empty;

    public IStash Stash { get; set; } = null!;

    public MediathekViewWebManager(MediathekClient client, int maxConcurrent = 3)
    {
        _client = client;
        _maxConcurrent = maxConcurrent;

        Receive<QueryMediathek>(HandleQuery);
        Receive<QueryMediathekCompleted>(HandleCompleted);
        Receive<QueryMediathekFailed>(HandleFailed);
    }

    private void HandleQuery(QueryMediathek query)
    {
        if (!_state.HasCapacity(_maxConcurrent))
        {
            _log.Debug("At capacity ({MaxConcurrent}), stashing query", _maxConcurrent);
            Stash.Stash();
            return;
        }

        _state = _state.Apply(new MediathekViewWebManagerState.RequestStarted());

        _client.QueryAsync(query).PipeTo(
            Self,
            Sender,
            success: result => result,
            failure: ex => new QueryMediathekFailed(ex));
    }

    private void HandleCompleted(QueryMediathekCompleted msg)
    {
        Sender.Tell(msg);
        SlotFreed();
    }

    private void HandleFailed(QueryMediathekFailed msg)
    {
        Telemetry.MediathekErrors.Add(1);
        _log.Warning(msg.Cause, "MediathekViewWeb query failed");
        Sender.Tell(msg);
        SlotFreed();
    }

    private void SlotFreed()
    {
        _state = _state.Apply(new MediathekViewWebManagerState.RequestCompleted());
        Stash.Unstash();
    }
}
