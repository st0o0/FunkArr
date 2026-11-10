using Akka.Actor;
using Akka.Event;
using FunkArr.Core.Actors;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;
using Terp.Akka;

namespace FunkArr.Feed.Crawl;

public sealed class FeedCrawlPageActor : ReceiveActor, IWithUnboundedStash
{
    public IStash Stash { get; set; } = null!;

    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _maxLead = TimeSpan.FromDays(90);

    private sealed record FetchCompleted(QueryMediathekCompleted Result, ProcessPage Request);
    private sealed record FetchFailed(string Reason, ProcessPage Request);
    private sealed record EvalCompleted(string RuleSetId, MediaType MediaType, MediathekItem[] Sources, SearchResultItem[] Items, double MinConfidence);
    private sealed record EvalFailed(string RuleSetId, string Reason);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _manager = Context.GetActor<IFeedCrawlManager>();
    private readonly IActorRef _mediathek = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _tvEvaluation = Context.GetActor<ITvEvaluationRegion>();
    private readonly IActorRef _movieEvaluation = Context.GetActor<IMovieEvaluationRegion>();

    private int _pendingGroups;
    private int _totalNewItems;
    private int _totalUnmatched;
    private bool _reachedEnd;
    private readonly List<FeedEntry> _accepted = [];
    private readonly Dictionary<RejectionReason, int> _rejectedByReason = [];

    public FeedCrawlPageActor()
    {
        Ready();
    }

    private void Ready()
    {
        Receive<ProcessPage>(HandleProcessPage);
    }

    private void HandleProcessPage(ProcessPage msg)
    {
        _pendingGroups = 0;
        _totalNewItems = 0;
        _totalUnmatched = 0;
        _reachedEnd = false;
        _accepted.Clear();
        _rejectedByReason.Clear();

        _mediathek.Ask<QueryMediathekResponse>(
                new QueryMediathek([], "filmlisteTimestamp", "desc", Future: true,
                    msg.Offset, msg.PageSize, 300, null),
                _mediathekTimeout)
            .PipeTo(Self,
                success: response => response switch
                {
                    QueryMediathekCompleted c => new FetchCompleted(c, msg),
                    QueryMediathekError e => new FetchFailed($"MediathekViewWeb query failed: {e.Cause.Message}", msg),
                    _ => new FetchFailed("MediathekViewWeb queue full", msg)
                },
                failure: ex => new FetchFailed(ex.Message, msg));

        Become(Fetching);
    }

    private void Fetching()
    {
        Receive<FetchCompleted>(HandleFetchCompleted);
        Receive<FetchFailed>(HandleFetchFailed);
        Receive<ProcessPage>(_ => Stash.Stash());
    }

    private void HandleFetchCompleted(FetchCompleted msg)
    {
        var request = msg.Request;
        var window = request.Window;
        var completed = msg.Result;
        var leadLimit = (DateTimeOffset.UtcNow + _maxLead).ToUnixTimeSeconds();

        var items = new List<MediathekItem>(completed.Items.Length);
        _reachedEnd = completed.Items.Length < request.PageSize;

        foreach (var item in completed.Items)
        {
            if (item.FilmlisteTimestamp <= window.LowerBound)
            {
                _reachedEnd = true;
                break;
            }

            if (item.FilmlisteTimestamp <= window.UpperBound)
            {
                items.Add(item);
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var groups = new Dictionary<string, (RuleSetTarget Target, List<MediathekItem> Items)>(StringComparer.Ordinal);

        foreach (var item in items)
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

            if (request.Lookup.Match(item.Topic) is not { } target)
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

        _totalNewItems = seen.Count;
        _totalUnmatched = seen.Count - groups.Values.Sum(g => g.Items.Count);

        if (groups.Count == 0)
        {
            ReplyAndBecomeReady();
            return;
        }

        _pendingGroups = groups.Count;

        foreach (var (ruleSetId, (target, groupItems)) in groups)
        {
            var mediaType = target.MediaType;
            var items2 = groupItems.ToArray();

            if (mediaType == MediaType.Movie)
            {
                _movieEvaluation.Ask<EvaluateMoviesResponse>(
                        new EvaluateMovies(Guid.NewGuid(), SearchSource.Feed,
                            items2, target.Ids.ImdbId, target.Ids.TmdbId),
                        _evaluationTimeout)
                    .PipeTo(Self,
                        success: response => response switch
                        {
                            EvaluateMoviesCompleted c => new EvalCompleted(ruleSetId, mediaType, items2, c.Items, request.MinConfidence),
                            EvaluateMoviesFailed f => new EvalFailed(ruleSetId, f.Cause.Message),
                            _ => new EvalFailed(ruleSetId, "unexpected movie evaluation response")
                        },
                        failure: ex => new EvalFailed(ruleSetId, ex.Message));
            }
            else
            {
                _tvEvaluation.Ask<EvaluateSeriesResponse>(
                        new EvaluateSeries(Guid.NewGuid(), SearchSource.Feed,
                            items2, target.Ids.TvdbId, target.Ids.ImdbId),
                        _evaluationTimeout)
                    .PipeTo(Self,
                        success: response => response switch
                        {
                            EvaluateSeriesCompleted c => new EvalCompleted(ruleSetId, mediaType, items2, c.Items, request.MinConfidence),
                            EvaluateSeriesFailed f => new EvalFailed(ruleSetId, f.Cause.Message),
                            _ => new EvalFailed(ruleSetId, "unexpected series evaluation response")
                        },
                        failure: ex => new EvalFailed(ruleSetId, ex.Message));
            }
        }

        Become(Evaluating);
    }

    private void HandleFetchFailed(FetchFailed msg)
    {
        _manager.Tell(new PageProcessed([], 0, 0, true, null, msg.Reason));
        Become(Ready);
        Stash.UnstashAll();
    }

    private void Evaluating()
    {
        Receive<EvalCompleted>(HandleEvalCompleted);
        Receive<EvalFailed>(HandleEvalFailed);
        Receive<ProcessPage>(_ => Stash.Stash());
    }

    private void HandleEvalCompleted(EvalCompleted result)
    {
        var gate = FeedGate.Evaluate(result.Items, result.Sources, result.MediaType, result.MinConfidence);

        _accepted.AddRange(gate.Accepted);
        foreach (var (reason, count) in gate.RejectedByReason)
        {
            Telemetry.Rejected.Add(count, new KeyValuePair<string, object?>("reason", reason.ToString()));
            _rejectedByReason[reason] = _rejectedByReason.GetValueOrDefault(reason) + count;
        }

        _pendingGroups--;
        if (_pendingGroups == 0)
        {
            ReplyAndBecomeReady();
        }
    }

    private void HandleEvalFailed(EvalFailed result)
    {
        _log.Warning("Evaluation failed for ruleset {RuleSetId}: {Reason}", result.RuleSetId, result.Reason);

        _pendingGroups--;
        if (_pendingGroups == 0)
        {
            ReplyAndBecomeReady();
        }
    }

    private void ReplyAndBecomeReady()
    {
        IReadOnlyDictionary<RejectionReason, int>? rejected = _rejectedByReason.Count > 0
            ? _rejectedByReason
            : null;

        _manager.Tell(new PageProcessed([.. _accepted], _totalNewItems, _totalUnmatched, _reachedEnd, rejected, null));
        Become(Ready);
        Stash.UnstashAll();
    }
}
