using Akka.Actor;
using Akka.Cluster.Sharding;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class MovieSearchWorker : ReceiveActor
{
    private MovieSearchWorkerState? _state;

    public MovieSearchWorker()
    {
        var mediathekManager = Context.GetActor<IMediathekManager>();
        var matchMagicManager = Context.GetActor<IMatchMagicManager>();
        var ruleSetResolver = Context.GetActor<IRuleSetResolver>();

        Receive<MovieSearchCommand>(cmd =>
        {
            _state = MovieSearchWorkerState.Empty.Apply(cmd);

            var hasQuery = !string.IsNullOrWhiteSpace(cmd.Query);
            var hasId = cmd.ImdbId is not null || cmd.TmdbId is not null;

            if (hasQuery)
            {
                QueryMediathek(mediathekManager, cmd.Query!, cmd.Offset, cmd.Limit);
            }
            else if (hasId)
            {
                ruleSetResolver.Ask<object>(
                        new ResolveRuleSet(null, ImdbId: cmd.ImdbId, TmdbId: cmd.TmdbId),
                        TimeSpan.FromSeconds(5))
                    .PipeTo(Self, Sender);
            }
            else
            {
                Sender.Tell(new SearchFailed(cmd.SearchId, "Movie search requires a query or media ID"));
                Context.Parent.Tell(new Passivate(PoisonPill.Instance));
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
                ruleSetResolver.Ask<object>(new ResolveRuleSet(topic), TimeSpan.FromSeconds(5))
                    .PipeTo(Self, Sender);
            }
            else if (_state.RuleSetId is not null)
            {
                StartScoring(matchMagicManager);
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
                QueryMediathek(mediathekManager, resolved.Topic, null, null);
            }
            else
            {
                StartScoring(matchMagicManager);
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

        Receive<ScoreCompleted>(scored =>
        {
            if (_state is null)
            {
                return;
            }

            Sender.Tell(_state.ToScoredResult(scored));
            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
        });

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

            Sender.Tell(new SearchFailed(_state.SearchId, failure.Cause.Message));
            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
        });
    }

    private void QueryMediathek(IActorRef mediathekManager, string query, int? offset, int? limit)
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

        mediathekManager.Ask<object>(msg, TimeSpan.FromSeconds(15))
            .PipeTo(Self, Sender);
    }

    private void StartScoring(IActorRef matchMagicManager)
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
        matchMagicManager.Ask<object>(
                new ScoreItems(requestId, _state.RuleSetId!, origin, candidates),
                TimeSpan.FromSeconds(10))
            .PipeTo(Self, Sender);
    }
}
