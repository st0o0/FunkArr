using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using FunkArr.Core.Actors;
using FunkArr.Feed.Eval;
using FunkArr.Feed.Page;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using Terp.Akka;

namespace FunkArr.Feed.Crawl;

public sealed class FeedCrawlWorker : ReceiveActor
{
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _publishTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _maxLead = TimeSpan.FromDays(90);

    private sealed record ProbeResult(long ListTimestamp);
    private sealed record ProbeFailed(string Reason);
    private sealed record RuleSetsLoaded(RegisteredRuleSetsResult Result);
    private sealed record RuleSetsFailed(string Reason);
    private sealed record PublishCompleted(int Published);
    private sealed record PublishFailed(string Reason);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _mediathek = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _store = Context.GetActor<IFeedStoreManager>();
    private readonly TimeProvider _timeProvider;
    private readonly IActorRef _pageFetchPool;
    private readonly IActorRef _evalPool;
    private readonly FeedCrawlWorkerState _state;

    public FeedCrawlWorker(CrawlInput input, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _state = new FeedCrawlWorkerState(input);

        _pageFetchPool = Context.ActorOf(
            Props.Create<FeedPageActor>()
                .WithRouter(new SmallestMailboxPool(Math.Max(1, input.PagePrefetch))),
            "page-pool");

        _evalPool = Context.ActorOf(
            Props.Create<FeedEvalActor>()
                .WithRouter(new SmallestMailboxPool(Math.Max(1, input.MaxConcurrency))),
            "eval-pool");

        StartProbe();
    }

    private void StartProbe()
    {
        _mediathek.Ask<QueryMediathekResponse>(
                new QueryMediathek([], "filmlisteTimestamp", "desc", Future: true, 0, 1, 300, null),
                _mediathekTimeout)
            .PipeTo(Self,
                success: response => response is QueryMediathekCompleted c
                    ? new ProbeResult(c.ListTimestamp)
                    : new ProbeFailed(response is QueryMediathekError e
                        ? $"MediathekViewWeb query failed: {e.Cause.Message}"
                        : "MediathekViewWeb queue full"),
                failure: ex => new ProbeFailed(ex.Message));

        Become(Probing);
    }

    private void Probing()
    {
        Receive<ProbeResult>(HandleProbeResult);
        Receive<ProbeFailed>(HandleProbeFailed);
    }

    private void HandleProbeResult(ProbeResult probe)
    {
        if (probe.ListTimestamp <= 0 || probe.ListTimestamp <= _state.Input.Cursor)
        {
            Complete(CrawlResult.ForUnchanged(probe.ListTimestamp,
                _timeProvider.GetElapsedTime(_state.Input.StartedAt)));
            return;
        }

        _state.ListTimestamp = probe.ListTimestamp;

        _ruleSetResolver.Ask<RegisteredRuleSetsResult>(
                new QueryRegisteredRuleSets(), _ruleSetTimeout)
            .PipeTo(Self,
                success: result => new RuleSetsLoaded(result),
                failure: ex => new RuleSetsFailed(ex.Message));

        Become(LoadingRuleSets);
    }

    private void HandleProbeFailed(ProbeFailed msg)
    {
        Complete(CrawlResult.ForFailed(0, 0, 0, 0, false,
            _timeProvider.GetElapsedTime(_state.Input.StartedAt), msg.Reason));
    }

    private void LoadingRuleSets()
    {
        Receive<RuleSetsLoaded>(HandleRuleSetsLoaded);
        Receive<RuleSetsFailed>(HandleRuleSetsFailed);
    }

    private void HandleRuleSetsLoaded(RuleSetsLoaded msg)
    {
        var input = _state.Input;
        var listTimestamp = _state.ListTimestamp;
        var lookup = RuleSetLookup.From(msg.Result.Entries);

        var cursor = input.Cursor;
        var catchUp = (long)input.MaxCatchUp.TotalSeconds;
        var window = cursor <= 0
            ? new CrawlWindow(Math.Max(0, listTimestamp - (long)input.InitialLookback.TotalSeconds), listTimestamp)
            : listTimestamp - catchUp is var earliest && cursor < earliest
                ? new CrawlWindow(earliest, listTimestamp, Truncated: true)
                : new CrawlWindow(cursor, listTimestamp);

        _state.Lookup = lookup;
        _state.Window = window;
        _log.Debug("Feed crawl probed: listTimestamp={ListTimestamp}, window=[{Lower},{Upper}], truncated={Truncated}",
            listTimestamp, window.LowerBound, window.UpperBound, window.Truncated);

        StartHarvesting();
    }

    private void HandleRuleSetsFailed(RuleSetsFailed msg)
    {
        Complete(CrawlResult.ForFailed(_state.ListTimestamp, 0, 0, 0, false,
            _timeProvider.GetElapsedTime(_state.Input.StartedAt), msg.Reason));
    }

    private void StartHarvesting()
    {
        var input = _state.Input;
        var initialRequests = Math.Min(input.PagePrefetch, input.MaxPages);
        _state.StartHarvesting(initialRequests);

        for (var i = 0; i < initialRequests; i++)
        {
            _pageFetchPool.Tell(new FetchPage(i * input.PageSize, _state.Window!, input.PageSize));
        }

        Become(Harvesting);
    }

    private void Harvesting()
    {
        Receive<PageResult>(HandlePageResult);
    }

    private void HandlePageResult(PageResult page)
    {
        _state.Apply(page);

        if (page.Error is null
            && !_state.ReachedEnd
            && _state.NextOffset / _state.Input.PageSize < _state.Input.MaxPages)
        {
            _state.FireNextPage();
            _pageFetchPool.Tell(new FetchPage(
                _state.NextOffset - _state.Input.PageSize, _state.Window!, _state.Input.PageSize));
        }

        if (_state.PendingPages > 0)
        {
            return;
        }

        if (_state.CollectedItems.Count > 0)
        {
            StartEvaluating();
        }
        else
        {
            Complete(_state.GetCrawlResult(_timeProvider.GetElapsedTime(_state.Input.StartedAt)));
        }
    }

    private void StartEvaluating()
    {
        var lookup = _state.Lookup!;
        var leadLimit = (_timeProvider.GetUtcNow() + _maxLead).ToUnixTimeSeconds();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var groups = new Dictionary<string, (RuleSetTarget Target, List<MediathekItem> Items)>(StringComparer.Ordinal);

        foreach (var item in _state.CollectedItems)
        {
            var url = item.UrlVideoHd ?? item.UrlVideo ?? item.UrlVideoLow;
            if (url is null)
            {
                continue;
            }

            if (item.Timestamp > leadLimit)
            {
                continue;
            }

            var stableId = FeedGate.StableIdFor(item.Channel, url);
            if (!seen.Add(stableId))
            {
                continue;
            }

            if (lookup.Match(item.Topic) is not { } target)
            {
                continue;
            }

            if (!groups.TryGetValue(target.RuleSetId, out var group))
            {
                group = (target, []);
                groups[target.RuleSetId] = group;
            }

            group.Items.Add(item);
        }

        if (groups.Count == 0)
        {
            var duration = _timeProvider.GetElapsedTime(_state.Input.StartedAt);
            Complete(CrawlResult.ForCompleted(_state.ListTimestamp,
                _state.CollectedItems.Count, _state.CollectedItems.Count, 0,
                _state.Window?.Truncated ?? false, duration));
            return;
        }

        _state.StartEvaluating(groups.Count);

        foreach (var (ruleSetId, (target, items)) in groups)
        {
            _evalPool.Tell(new EvalGroup(ruleSetId, target.MediaType, target.Ids,
                [.. items], _state.Input.MinConfidence));
        }

        Become(Evaluating);
    }

    private void Evaluating()
    {
        Receive<GroupResult>(HandleGroupResult);
    }

    private void HandleGroupResult(GroupResult result)
    {
        _state.Apply(result);

        if (_state.PendingGroups == 0)
        {
            StartPublishing();
        }
    }

    private void StartPublishing()
    {
        var entries = _state.AcceptedEntries;

        if (entries.Count == 0)
        {
            Complete(_state.GetCrawlResult(_timeProvider.GetElapsedTime(_state.Input.StartedAt)));
            return;
        }

        _store.Ask<PublishFeedEntriesResponse>(
                new PublishFeedEntries([.. entries]), _publishTimeout)
            .PipeTo(Self,
                success: response => response switch
                {
                    PublishFeedEntriesCompleted c => new PublishCompleted(c.Published),
                    PublishFeedEntriesFailed f => new PublishFailed(f.Cause.Message),
                    _ => new PublishFailed("unexpected publish response")
                },
                failure: ex => new PublishFailed(ex.Message));

        Become(Publishing);
    }

    private void Publishing()
    {
        Receive<PublishCompleted>(_ =>
            Complete(_state.GetCrawlResult(_timeProvider.GetElapsedTime(_state.Input.StartedAt))));
        Receive<PublishFailed>(msg =>
            Complete(CrawlResult.ForFailed(_state.ListTimestamp,
                _state.CollectedItems.Count,
                _state.CollectedItems.Count - _state.TotalGroups,
                0, _state.Window?.Truncated ?? false,
                _timeProvider.GetElapsedTime(_state.Input.StartedAt), msg.Reason)));
    }

    private void Complete(CrawlResult result)
    {
        Context.Parent.Tell(result);
        Context.Stop(Self);
    }
}
