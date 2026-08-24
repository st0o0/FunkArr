using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using FunkArr.Search.Matching;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

public sealed class BrowseActor : ReceiveActor
{
    public sealed record Browse;
    public sealed record BrowseResponse(IReadOnlyList<SearchResult> Results);

    private sealed record FetchedEnvelope(ItemsFetched Result);
    private sealed record FetchFailed(Exception Error);

    private readonly IReadOnlyActorRegistry _registry;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly List<IActorRef> _pendingCallers = [];
    private IReadOnlyList<SearchResult>? _cached;
    private DateTimeOffset _cachedAt;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public BrowseActor(IReadOnlyActorRegistry registry)
    {
        _registry = registry;

        Receive<Browse>(_ => HandleBrowse());
        Receive<FetchedEnvelope>(HandleFetched);
        Receive<FetchFailed>(HandleFetchFailed);
    }

    private void HandleBrowse()
    {
        if (_cached is not null && DateTimeOffset.UtcNow - _cachedAt < CacheTtl)
        {
            Sender.Tell(new BrowseResponse(_cached));
            return;
        }

        _pendingCallers.Add(Sender);

        if (_pendingCallers.Count > 1)
        {
            return;
        }

        var gateway = _registry.Get<MediathekGatewayActor>();
        gateway.Ask<ItemsFetched>(new FetchItems(""), TimeSpan.FromSeconds(30))
            .PipeTo(Self,
                success: r => new FetchedEnvelope(r),
                failure: ex => new FetchFailed(ex));
    }

    private void HandleFetched(FetchedEnvelope envelope)
    {
        var results = MatchingPipeline.FilterResults(envelope.Result.Items);

        _cached = results;
        _cachedAt = DateTimeOffset.UtcNow;

        var response = new BrowseResponse(results);
        foreach (var caller in _pendingCallers)
        {
            caller.Tell(response);
        }

        _pendingCallers.Clear();
    }

    private void HandleFetchFailed(FetchFailed failure)
    {
        _log.Warning(failure.Error, "Browse fetch failed");
        var response = new BrowseResponse(_cached ?? []);
        foreach (var caller in _pendingCallers)
        {
            caller.Tell(response);
        }

        _pendingCallers.Clear();
    }
}
