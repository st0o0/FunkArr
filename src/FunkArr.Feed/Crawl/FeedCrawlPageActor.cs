using Akka.Actor;
using Akka.Event;
using FunkArr.Core.Actors;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;
using Terp.Akka;

namespace FunkArr.Feed.Crawl;

public sealed class FeedCrawlPageActor : ReceiveActor, IWithUnboundedStash
{
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _maxLead = TimeSpan.FromDays(90);

    private sealed record FetchCompleted(QueryMediathekCompleted Result, ProcessPage Request);
    private sealed record FetchFailed(string Reason);
    private sealed record EvalCompleted(string RuleSetId, MediaType MediaType, MediathekItem[] Sources, SearchResultItem[] Items, double MinConfidence);
    private sealed record EvalFailed(string RuleSetId, string Reason);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _mediathek = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _tvEvaluation = Context.GetActor<ITvEvaluationRegion>();
    private readonly IActorRef _movieEvaluation = Context.GetActor<IMovieEvaluationRegion>();

    private FeedCrawlPageActorState _state = FeedCrawlPageActorState.Empty;
    private IActorRef _replyTo = ActorRefs.Nobody;

    public IStash Stash { get; set; } = null!;

    public FeedCrawlPageActor()
    {
        Ready();
    }

    private void Ready()
    {
        Receive<ProcessPage>(msg =>
        {
            _replyTo = Sender;
            _state = FeedCrawlPageActorState.Empty;

            _mediathek.Ask<QueryMediathekResponse>(
                    new QueryMediathek([], "filmlisteTimestamp", "desc", Future: true,
                        msg.Offset, msg.PageSize, 300, null),
                    _mediathekTimeout)
                .PipeTo(Self,
                    success: response => response switch
                    {
                        QueryMediathekCompleted c => new FetchCompleted(c, msg),
                        QueryMediathekError e => new FetchFailed($"MediathekViewWeb query failed: {e.Cause.Message}"),
                        _ => new FetchFailed("MediathekViewWeb queue full")
                    },
                    failure: ex => new FetchFailed(ex.Message));

            Become(Fetching);
        });
    }

    private void Fetching()
    {
        Receive<ProcessPage>(_ => Stash.Stash());

        Receive<FetchCompleted>(msg =>
        {
            var request = msg.Request;
            var completed = msg.Result;
            var leadLimit = (DateTimeOffset.UtcNow + _maxLead).ToUnixTimeSeconds();

            var items = new List<MediathekItem>(completed.Items.Length);
            var reachedEnd = completed.Items.Length < request.PageSize;

            foreach (var item in completed.Items)
            {
                if (item.FilmlisteTimestamp <= request.LowerBound)
                {
                    reachedEnd = true;
                    break;
                }

                if (item.FilmlisteTimestamp <= request.UpperBound)
                {
                    items.Add(item);
                }
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var groups = new Dictionary<string, (RuleSetTarget Target, List<MediathekItem> Items)>(StringComparer.Ordinal);
            var topicTargets = request.TopicTargets;

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

                if (!topicTargets.TryGetValue(item.Topic, out var target))
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

            _state.Apply(new FilterResult(seen.Count, seen.Count - groups.Values.Sum(g => g.Items.Count), reachedEnd, groups.Count));

            if (groups.Count == 0)
            {
                ReplyAndBecomeReady();
                return;
            }

            foreach (var (ruleSetId, (target, groupItems)) in groups)
            {
                var mediaType = target.MediaType;
                var evalItems = groupItems.ToArray();

                if (mediaType == MediaType.Movie)
                {
                    _movieEvaluation.Ask<EvaluateMoviesResponse>(
                            new EvaluateMovies(Guid.NewGuid(), SearchSource.Feed,
                                evalItems, target.Ids.ImdbId, target.Ids.TmdbId),
                            _evaluationTimeout)
                        .PipeTo(Self,
                            success: response => response switch
                            {
                                EvaluateMoviesCompleted c => new EvalCompleted(ruleSetId, mediaType, evalItems, c.Items, request.MinConfidence),
                                EvaluateMoviesFailed f => new EvalFailed(ruleSetId, f.Cause.Message),
                                _ => new EvalFailed(ruleSetId, "unexpected movie evaluation response")
                            },
                            failure: ex => new EvalFailed(ruleSetId, ex.Message));
                }
                else
                {
                    _tvEvaluation.Ask<EvaluateSeriesResponse>(
                            new EvaluateSeries(Guid.NewGuid(), SearchSource.Feed,
                                evalItems, target.Ids.TvdbId, target.Ids.ImdbId),
                            _evaluationTimeout)
                        .PipeTo(Self,
                            success: response => response switch
                            {
                                EvaluateSeriesCompleted c => new EvalCompleted(ruleSetId, mediaType, evalItems, c.Items, request.MinConfidence),
                                EvaluateSeriesFailed f => new EvalFailed(ruleSetId, f.Cause.Message),
                                _ => new EvalFailed(ruleSetId, "unexpected series evaluation response")
                            },
                            failure: ex => new EvalFailed(ruleSetId, ex.Message));
                }
            }

            Become(Evaluating);
        });

        Receive<FetchFailed>(msg =>
        {
            _replyTo.Tell(new ProcessPageFailed(msg.Reason));
            Stash.UnstashAll();
            Become(Ready);
        });
    }

    private void Evaluating()
    {
        Receive<ProcessPage>(_ => Stash.Stash());

        Receive<EvalCompleted>(result =>
        {
            var gate = FeedGate.Evaluate(result.Items, result.Sources, result.MediaType, result.MinConfidence);

            foreach (var (reason, count) in gate.RejectedByReason)
            {
                Telemetry.Rejected.Add(count, new KeyValuePair<string, object?>("reason", reason.ToString()));
            }

            _state.Apply(gate);

            if (_state.IsComplete)
            {
                ReplyAndBecomeReady();
            }
        });

        Receive<EvalFailed>(result =>
        {
            _log.Warning("Evaluation failed for ruleset {RuleSetId}: {Reason}", result.RuleSetId, result.Reason);
            _state.Apply(new EvalFailure());

            if (_state.IsComplete)
            {
                ReplyAndBecomeReady();
            }
        });
    }

    private void ReplyAndBecomeReady()
    {
        _replyTo.Tell(_state.GetResult());
        Stash.UnstashAll();
        Become(Ready);
    }
}
