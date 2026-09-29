using System.Diagnostics;
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

public sealed class MovieSearchWorker : ReceiveActor, IWithTimers
{
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _scoringTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _enrichmentTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan[] _retryBackoff = [TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1500)];
    private const int MaxRetries = 2;

    private sealed record RetryMediathekQuery(int Attempt);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly MovieSearchWorkerState _state;

    private readonly IActorRef _mediathekManager = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _scoringManager = Context.GetActor<IScoringManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _metadataResolver = Context.GetActor<IEnrichmentManager>();
    private readonly IActorRef _historyRegion = Context.GetActor<IHistoryRegion>();

    private readonly Stopwatch _stopwatch = new();

    public ITimerScheduler Timers { get; set; } = null!;

    private QueryMediathek? _lastQuery;

    public MovieSearchWorker(string nttId)
    {
        _state = MovieSearchWorkerState.From(nttId);
        Receive<SearchMovie>(cmd =>
        {
            _log.Info("Movie search started: Query={Query}, TmdbId={TmdbId}", cmd.Query, cmd.TmdbId);
            _stopwatch.Restart();
            _state.Apply(cmd, Sender);

            switch (cmd)
            {
                case { ImdbId: not null } or { TmdbId: not null }
                    when _state.TryGetRuleSetRequest(out var request):
                    _ruleSetResolver.Ask<ResolveRuleSetResponse>(request, _ruleSetTimeout)
                        .PipeTo(Self, failure: ex => new RuleSetFailed(ex));
                    Become(ResolvingRuleSet);
                    break;

                default:
                    _state.TryGetMediathekQuery(out var query);
                    AskMediathek(query!);
                    break;
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

        Receive<QueryMediathekQueueFull>(_ =>
        {
            if (_state.Attempt < MaxRetries)
            {
                var delay = _retryBackoff[_state.Attempt];
                _log.Info("Movie search {SearchId} queue full, retry {Attempt} in {Delay}ms",
                    _state.SearchId, _state.Attempt + 1, delay.TotalMilliseconds);
                Timers.StartSingleTimer("mediathek-retry",
                    new RetryMediathekQuery(_state.Attempt + 1), delay);
            }
            else
            {
                _log.Warning("Movie search {SearchId} queue full, retries exhausted", _state.SearchId);
                Telemetry.Timeouts.Add(1);
                Reply(new SearchMovieFailed(_state.SearchId,
                    new InvalidOperationException("MediathekViewWeb query queue full after retries")));
            }
        });

        Receive<RetryMediathekQuery>(msg =>
        {
            _state.Attempt = msg.Attempt;
            _mediathekManager.Ask<QueryMediathekResponse>(_lastQuery!, _mediathekTimeout)
                .PipeTo(Self, failure: ex => new QueryMediathekError(ex));
        });

        Receive<QueryMediathekError>(failed =>
        {
            _log.Warning(failed.Cause, "Movie search {SearchId} mediathek query failed", _state.SearchId);
            Reply(new SearchMovieFailed(_state.SearchId, failed.Cause));
        });
    }

    private void ResolvingRuleSet()
    {
        Receive<RuleSetResolved>(resolved =>
        {
            _state.Apply(resolved.RuleSetId, resolved.MediaName, resolved.Enrichment);

            if (_state.Sources.Length == 0)
            {
                _state.TryGetMediathekQueryForTopic(resolved.Topic, out var query);
                AskMediathek(query!);
            }
            else if (_state.TryGetScoringRequest(out var scoringRequest))
            {
                _scoringManager.Ask<ScoreItemsResponse>(scoringRequest, _scoringTimeout)
                    .PipeTo(Self, failure: ex => new ScoringFailed(ex));
                Become(Scoring);
            }
        });

        Receive<RuleSetFailed>(_ => Reply(_state.ToSearchCompleted()));
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

    private void AskMediathek(QueryMediathek query)
    {
        _lastQuery = query;
        _state.Attempt = 0;
        _mediathekManager.Ask<QueryMediathekResponse>(query, _mediathekTimeout)
            .PipeTo(Self, failure: ex => new QueryMediathekError(ex));
        Become(Querying);
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
        _stopwatch.Stop();
        var sourceTag = new KeyValuePair<string, object?>("source", _state.Source.ToString().ToLowerInvariant());
        var typeTag = new KeyValuePair<string, object?>("type", "movie");
        Telemetry.SearchRequests.Add(1, sourceTag);
        Telemetry.SearchDuration.Record(_stopwatch.Elapsed.TotalSeconds, sourceTag, typeTag);
        if (response is SearchMovieCompleted completed)
        {
            Telemetry.ResultsPerRequest.Record(completed.Items.Length);
            if (completed.Items.Length > 0)
            {
                Telemetry.SearchMatches.Add(1, sourceTag);
            }
            else
            {
                Telemetry.SearchNoMatch.Add(1, sourceTag);
            }
        }
        else
        {
            Telemetry.SearchNoMatch.Add(1, sourceTag);
        }

        _state.ReplyTo.Tell(response);
    }
}
