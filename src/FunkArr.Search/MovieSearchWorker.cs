using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class MovieSearchWorker : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private MovieSearchWorkerState _state = null!;

    private readonly IActorRef _mediathekManager = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _scoringManager = Context.GetActor<IScoringManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _metadataResolver = Context.GetActor<IEnrichmentManager>();

    public MovieSearchWorker()
    {
        Receive<SearchMovie>(cmd =>
        {
            _log.Info("Movie search started: Query={Query}, TmdbId={TmdbId}", cmd.Query, cmd.TmdbId);
            _state = MovieSearchWorkerState.From(cmd, Sender);

            var hasQuery = !string.IsNullOrWhiteSpace(cmd.Query);
            var hasId = cmd.ImdbId is not null || cmd.TmdbId is not null;

            if (hasQuery)
            {
                QueryMediathek(cmd.Query!, cmd.Offset, cmd.Limit);
                Become(Querying);
            }
            else if (hasId)
            {
                _ruleSetResolver.Ask<ResolveRuleSetResponse>(
                        new ResolveRuleSet(null, ImdbId: cmd.ImdbId, TmdbId: cmd.TmdbId),
                        TimeSpan.FromSeconds(5))
                    .PipeTo(Self, failure: ex => new RuleSetFailed(ex));
                Become(ResolvingRuleSet);
            }
            else
            {
                QueryMediathek("", cmd.Offset, cmd.Limit);
                Become(Querying);
            }
        });
    }

    private void Querying()
    {
        Receive<QueryMediathekCompleted>(result =>
        {
            _state = _state.Apply(result);

            var topic = result.Items.Length > 0 ? result.Items[0].Topic : null;
            if (topic is not null && _state.RuleSetId is null)
            {
                _ruleSetResolver.Ask<ResolveRuleSetResponse>(new ResolveRuleSet(topic), TimeSpan.FromSeconds(5))
                    .PipeTo(Self, failure: ex => new RuleSetFailed(ex));
                Become(ResolvingRuleSet);
            }
            else if (_state.RuleSetId is not null && _state.RawItems.Length > 0)
            {
                StartScoring();
            }
            else
            {
                ReplyCompleted(SearchPipeline.BuildUnscoredItems(BuildContext()));
            }
        });

        Receive<QueryMediathekFailed>(failed =>
        {
            _log.Warning(failed.Cause, "Movie search {SearchId} mediathek query failed", _state.SearchId);
            Reply(new SearchMovieFailed(_state.SearchId, failed.Cause));
        });
    }

    private void ResolvingRuleSet()
    {
        Receive<RuleSetResolved>(resolved =>
        {
            _state = _state.ApplyRuleSet(resolved.RuleSetId, resolved.MediaName);

            if (_state.RawItems.Length == 0)
            {
                QueryMediathek(resolved.Topic, null, null);
                Become(Querying);
            }
            else
            {
                StartScoring();
            }
        });

        Receive<RuleSetFailed>(failed =>
        {
            if (_state.RawItems.Length == 0)
            {
                ReplyCompleted([]);
            }
            else
            {
                ReplyCompleted(SearchPipeline.BuildUnscoredItems(BuildContext()));
            }
        });
    }

    private void Scoring()
    {
        Receive<ScoreCompleted>(scored =>
        {
            var hasIds = _state.ImdbId is not null || _state.TmdbId is not null;
            var hasMatched = scored.Results.Any(s => s.Matched);

            if (!hasIds || !hasMatched)
            {
                ReplyCompleted(SearchPipeline.BuildScoredItems(BuildContext(), scored));
                return;
            }

            _state = _state.WithScoredResults(scored);

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

            _metadataResolver.Ask<EnrichMoviesResponse>(
                    new EnrichMovies(_state.ImdbId, _state.TmdbId, candidates),
                    TimeSpan.FromSeconds(10))
                .PipeTo(Self, failure: ex => new EnrichMoviesFailed(ex));
            Become(Enriching);
        });

        Receive<ScoringFailed>(failed =>
        {
            _log.Warning(failed.Cause, "Movie search {SearchId} scoring failed", _state.SearchId);
            Reply(new SearchMovieFailed(_state.SearchId, failed.Cause));
        });
    }

    private void Enriching()
    {
        Receive<EnrichMoviesCompleted>(enriched =>
        {
            var enrichedMap = enriched.Movies.ToDictionary(m => m.Index);
            ReplyCompleted(SearchPipeline.BuildScoredItems(BuildContext(), _state.ScoredResults!, enrichedMap));
        });

        Receive<EnrichMoviesFailed>(_ =>
        {
            ReplyCompleted(SearchPipeline.BuildScoredItems(BuildContext(), _state.ScoredResults!));
        });
    }

    private void Reply(SearchMovieResponse response)
    {
        _state.ReplyTo.Tell(response);
    }

    private void ReplyCompleted(SearchResultItem[] items)
    {
        Reply(new SearchMovieCompleted(_state.SearchId, items, items.Length));
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

        _mediathekManager.Ask<QueryMediathekResponse>(msg, TimeSpan.FromSeconds(15))
            .PipeTo(Self, failure: ex => new QueryMediathekFailed(ex));
    }

    private void StartScoring()
    {
        var candidates = _state.RawItems.Select(item => new ScoreCandidate(
            item.Title, item.Topic, item.Channel, item.Duration,
            item.ResolveQuality(),
            item.Description, item.Timestamp)).ToArray();

        var requestId = Guid.NewGuid();
        var origin = new ScoringOrigin(_state.Source, _state.RawItems[0].Topic);
        _scoringManager.Ask<ScoreItemsResponse>(
                new ScoreItems(requestId, _state.RuleSetId!, origin, candidates),
                TimeSpan.FromSeconds(10))
            .PipeTo(Self, failure: ex => new ScoringFailed(ex));
        Become(Scoring);
    }

    private SearchContext BuildContext() =>
        new(_state.SearchId, _state.RawItems, _state.MediaName,
            null, _state.ImdbId, _state.TmdbId, "movie");
}
