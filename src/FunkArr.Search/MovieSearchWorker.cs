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
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _scoringTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _enrichmentTimeout = TimeSpan.FromSeconds(10);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly MovieSearchWorkerState _state = new();

    private readonly IActorRef _mediathekManager = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _scoringManager = Context.GetActor<IScoringManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _metadataResolver = Context.GetActor<IEnrichmentManager>();
    private readonly IActorRef _historyRegion = Context.GetActor<IHistoryRegion>();

    private readonly System.Diagnostics.Stopwatch _searchStopwatch = new();

    public MovieSearchWorker()
    {
        Receive<SearchMovie>(cmd =>
        {
            _log.Info("Movie search started: Query={Query}, TmdbId={TmdbId}", cmd.Query, cmd.TmdbId);
            _searchStopwatch.Restart();
            _state.Init(cmd, Sender);

            var hasQuery = !string.IsNullOrWhiteSpace(cmd.Query);
            var hasId = cmd.ImdbId is not null || cmd.TmdbId is not null;

            if (hasQuery)
            {
                _state.TryGetMediathekQuery(out var query);
                _mediathekManager.Ask<QueryMediathekResponse>(query, _mediathekTimeout)
                    .PipeTo(Self, failure: ex => new QueryMediathekFailed(ex));
                Become(Querying);
            }
            else if (hasId)
            {
                if (_state.TryGetRuleSetRequest(out var request))
                {
                    _ruleSetResolver.Ask<ResolveRuleSetResponse>(request, _ruleSetTimeout)
                        .PipeTo(Self, failure: ex => new RuleSetFailed(ex));
                    Become(ResolvingRuleSet);
                }
            }
            else
            {
                _state.TryGetMediathekQuery(out var query);
                _mediathekManager.Ask<QueryMediathekResponse>(query, _mediathekTimeout)
                    .PipeTo(Self, failure: ex => new QueryMediathekFailed(ex));
                Become(Querying);
            }
        });
    }

    private void Querying()
    {
        Receive<QueryMediathekCompleted>(result =>
        {
            _state.Apply(result);

            if (_state.TryGetRuleSetRequest(out var ruleSetRequest))
            {
                _ruleSetResolver.Ask<ResolveRuleSetResponse>(ruleSetRequest, _ruleSetTimeout)
                    .PipeTo(Self, failure: ex => new RuleSetFailed(ex));
                Become(ResolvingRuleSet);
            }
            else if (_state.TryGetScoringRequest(out var scoringRequest))
            {
                _scoringManager.Ask<ScoreItemsResponse>(scoringRequest, _scoringTimeout)
                    .PipeTo(Self, failure: ex => new ScoringFailed(ex));
                Become(Scoring);
            }
            else
            {
                Reply(_state.ToSearchCompleted());
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
            _state.ApplyRuleSet(resolved.RuleSetId, resolved.MediaName, resolved.Enrichment);

            if (_state.Sources.Length == 0)
            {
                _state.TryGetMediathekQueryForTopic(resolved.Topic, out var query);
                _mediathekManager.Ask<QueryMediathekResponse>(query, _mediathekTimeout)
                    .PipeTo(Self, failure: ex => new QueryMediathekFailed(ex));
                Become(Querying);
            }
            else if (_state.TryGetScoringRequest(out var scoringRequest))
            {
                _scoringManager.Ask<ScoreItemsResponse>(scoringRequest, _scoringTimeout)
                    .PipeTo(Self, failure: ex => new ScoringFailed(ex));
                Become(Scoring);
            }
        });

        Receive<RuleSetFailed>(_ =>
        {
            Reply(_state.ToSearchCompleted());
        });
    }

    private void Scoring()
    {
        Receive<ScoreCompleted>(scored =>
        {
            _state.Apply(scored);

            if (_state.TryGetEnrichmentRequest(out var enrichRequest))
            {
                _metadataResolver.Ask<EnrichMoviesResponse>(enrichRequest, _enrichmentTimeout)
                    .PipeTo(Self, failure: ex => new EnrichMoviesFailed(ex));
                Become(Enriching);
                return;
            }

            RecordHistory();
            Reply(_state.ToSearchCompleted());
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
            _state.Apply(enriched);
            _state.MergeEnrichmentIntoTraces(enriched.Movies);
            RecordHistory();
            Reply(_state.ToSearchCompleted());
        });

        Receive<EnrichMoviesFailed>(_ =>
        {
            RecordHistory();
            Reply(_state.ToSearchCompleted());
        });
    }

    private void RecordHistory()
    {
        var record = _state.BuildRecordHistory();
        if (record is not null)
        {
            _historyRegion.Tell(record);
        }
    }

    private void Reply(SearchMovieResponse response)
    {
        _searchStopwatch.Stop();
        Telemetry.SearchDuration.Record(_searchStopwatch.Elapsed.TotalSeconds);
        if (response is SearchMovieCompleted completed)
        {
            Telemetry.SearchResults.Record(completed.Items.Length);
        }

        _state.ReplyTo.Tell(response);
    }
}
