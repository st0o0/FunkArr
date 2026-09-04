using Akka.Actor;
using Akka.Cluster.Sharding;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.MetadataResolver;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class MovieSearchWorker : ReceiveActor
{
    private MovieSearchWorkerState? _state;
    private ScoreCompleted? _scoredResults;
    private IActorRef? _originalSender;

    private readonly IActorRef _mediathekManager = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _matchMagicManager = Context.GetActor<IMatchMagicManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _metadataResolver = Context.GetActor<IMetadataResolver>();

    public MovieSearchWorker()
    {

        Receive<MovieSearchCommand>(cmd =>
        {
            _state = MovieSearchWorkerState.Empty.Apply(cmd);

            var hasQuery = !string.IsNullOrWhiteSpace(cmd.Query);
            var hasId = cmd.ImdbId is not null || cmd.TmdbId is not null;

            if (hasQuery)
            {
                QueryMediathek(cmd.Query!, cmd.Offset, cmd.Limit);
            }
            else if (hasId)
            {
                _ruleSetResolver.Ask<IRuleSetResponse>(
                        new ResolveRuleSet(null, ImdbId: cmd.ImdbId, TmdbId: cmd.TmdbId),
                        TimeSpan.FromSeconds(5))
                    .PipeTo(Self, Sender);
            }
            else
            {
                QueryMediathek("", cmd.Offset, cmd.Limit);
            }
        });

        Receive<MediathekQueryCompleted>(result =>
        {
            if (_state is null)
            {
                return;
            }

            _state = _state.Apply(result);

            var topic = result.Items.Length > 0 ? result.Items[0].Topic : null;
            if (topic is not null && _state.RuleSetId is null)
            {
                _ruleSetResolver.Ask<IRuleSetResponse>(new ResolveRuleSet(topic), TimeSpan.FromSeconds(5))
                    .PipeTo(Self, Sender);
            }
            else if (_state.RuleSetId is not null)
            {
                StartScoring();
            }
            else
            {
                Sender.Tell(_state.ToUnscoredResult());
                Context.Parent.Tell(new Passivate(PoisonPill.Instance));
            }
        });

        Receive<RuleSetResolved>(resolved =>
        {
            if (_state is null)
            {
                return;
            }

            _state = _state.ApplyRuleSet(resolved.RuleSetId);

            if (_state.RawItems.Length == 0)
            {
                QueryMediathek(resolved.Topic, null, null);
            }
            else
            {
                StartScoring();
            }
        });

        Receive<RuleSetNotFound>(_ =>
        {
            if (_state is null)
            {
                return;
            }

            if (_state.RawItems.Length == 0)
            {
                Sender.Tell(new SearchCompleted(_state.SearchId, [], 0));
            }
            else
            {
                Sender.Tell(_state.ToUnscoredResult());
            }

            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
        });

        Receive<ScoreCompleted>(HandleScoreCompleted);
        Receive<MoviesResolved>(HandleMoviesResolved);
        Receive<MovieResolutionFailed>(HandleMovieResolutionFailed);

        Receive<MediathekQueryFailed>(failed =>
        {
            if (_state is null)
            {
                return;
            }

            Sender.Tell(new SearchFailed(_state.SearchId, failed.Reason));
            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
        });

        Receive<Status.Failure>(failure =>
        {
            if (_state is null)
            {
                return;
            }

            if (_originalSender is not null && _scoredResults is not null)
            {
                _originalSender.Tell(_state.ToScoredResult(_scoredResults));
                _originalSender = null;
                _scoredResults = null;
                Context.Parent.Tell(new Passivate(PoisonPill.Instance));
                return;
            }

            Sender.Tell(new SearchFailed(_state.SearchId, failure.Cause.Message));
            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
        });
    }

    private void HandleScoreCompleted(ScoreCompleted scored)
    {
        if (_state is null)
        {
            return;
        }

        var hasIds = _state.ImdbId is not null || _state.TmdbId is not null;
        var hasMatched = scored.Results.Any(s => s.Matched);

        if (!hasIds || !hasMatched)
        {
            Sender.Tell(_state.ToScoredResult(scored));
            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
            return;
        }

        _scoredResults = scored;
        _originalSender = Sender;

        var candidates = scored.Results
            .Where(s => s.Matched)
            .Select(s =>
            {
                var raw = _state.RawItems[s.Index];
                return new MovieCandidate(
                    s.Index, raw.Title,
                    s.Metadata?.AiredAt, raw.Duration);
            })
            .ToArray();

        _metadataResolver.Ask<IMovieResolutionResponse>(
                new ResolveMovie(_state.ImdbId, _state.TmdbId, candidates),
                TimeSpan.FromSeconds(10))
            .PipeTo(Self);
    }

    private void HandleMoviesResolved(MoviesResolved resolved)
    {
        if (_state is null || _scoredResults is null || _originalSender is null)
        {
            return;
        }

        var resolvedMap = resolved.Movies.ToDictionary(m => m.Index);
        _originalSender.Tell(_state.ToScoredResult(_scoredResults, resolvedMap));
        _originalSender = null;
        _scoredResults = null;
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }

    private void HandleMovieResolutionFailed(MovieResolutionFailed _)
    {
        if (_state is null || _scoredResults is null || _originalSender is null)
        {
            return;
        }

        _originalSender.Tell(_state.ToScoredResult(_scoredResults));
        _originalSender = null;
        _scoredResults = null;
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }

    private void QueryMediathek(string query, int? offset, int? limit)
    {
        var fields = new List<MediathekQueryField>();
        if (!string.IsNullOrWhiteSpace(query))
        {
            fields.Add(new MediathekQueryField(["title", "topic"], query));
        }

        var msg = new QueryMediathek(
            Fields: fields.ToArray(),
            SortBy: "timestamp",
            SortOrder: "desc",
            Future: false,
            Offset: offset ?? 0,
            Size: limit ?? 50,
            DurationMin: 3600,
            DurationMax: null);

        _mediathekManager.Ask<IMediathekResponse>(msg, TimeSpan.FromSeconds(15))
            .PipeTo(Self, Sender);
    }

    private void StartScoring()
    {
        if (_state is null)
        {
            return;
        }

        var candidates = _state.RawItems.Select(item => new ScoreCandidate(
            item.Title, item.Topic, item.Channel, item.Duration,
            MovieSearchWorkerStateExtensions.ResolveQuality(item),
            item.Description, item.Timestamp)).ToArray();

        var requestId = Guid.NewGuid();
        var origin = new ScoringOrigin("radarr", _state.RawItems[0].Topic);
        _matchMagicManager.Ask<ScoreCompleted>(
                new ScoreItems(requestId, _state.RuleSetId!, origin, candidates),
                TimeSpan.FromSeconds(10))
            .PipeTo(Self, Sender);
    }
}
