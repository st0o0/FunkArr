using System.Diagnostics;
using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;
using Servus.Akka;
using Servus.Resilience;

namespace FunkArr.Search;

public sealed class MovieSearchWorker : ReceiveActor, IWithTimers
{
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(40);

    private static readonly BackoffPolicy _retryBackoff =
        Backoff.Create(TimeSpan.FromMilliseconds(500), multiplier: 3.0, maxDelay: TimeSpan.FromMilliseconds(1500));

    private const int MaxRetries = 2;

    private sealed record RetryMediathekQuery(int Attempt);

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly MovieSearchWorkerState _state;

    private readonly IActorRef _mediathekManager = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _evaluationRegion = Context.GetActor<IMovieEvaluationRegion>();

    private readonly Stopwatch _stopwatch = new();

    public ITimerScheduler Timers { get; set; } = null!;

    public MovieSearchWorker(string nttId)
    {
        _state = MovieSearchWorkerState.From(nttId);

        Receive<SearchMovie>(cmd =>
        {
            _log.Info("Movie search started: Query={Query}, TmdbId={TmdbId}", cmd.Query, cmd.TmdbId);
            _stopwatch.Restart();
            _state.Apply(cmd, Sender);

            if (_state.TryGetRuleSetRequest(out var request))
            {
                _ruleSetResolver.Ask<ResolveRuleSetResponse>(request, _ruleSetTimeout)
                    .PipeTo(Self, failure: ex => new RuleSetFailed(ex));
                Become(ResolvingRuleSet);
                return;
            }

            QueryMediathek();
        });
    }

    private void ResolvingRuleSet()
    {
        Receive<RuleSetResolved>(resolved =>
        {
            _state.ApplyRuleSet(resolved);
            QueryMediathek();
        });

        Receive<RuleSetFailed>(_ => Reply(_state.Empty()));
    }

    private void Querying()
    {
        Receive<QueryMediathekCompleted>(result =>
        {
            _state.Apply(result);

            if (_state.Items.Length == 0)
            {
                Reply(_state.Empty());
                return;
            }

            _evaluationRegion.Ask<EvaluateMoviesResponse>(_state.ToEvaluation(Guid.NewGuid()), _evaluationTimeout)
                .PipeTo(Self, failure: ex => new EvaluateMoviesFailed(_state.SearchId, ex));
            Become(Evaluating);
        });

        Receive<QueryMediathekQueueFull>(_ =>
        {
            if (_state.Attempt < MaxRetries)
            {
                var delay = _retryBackoff.DelayWithJitter(_state.Attempt);
                _log.Info("Movie search {SearchId} queue full, retry {Attempt} in {Delay}ms",
                    _state.SearchId, _state.Attempt + 1, delay.TotalMilliseconds);
                Timers.StartSingleTimer("mediathek-retry", new RetryMediathekQuery(_state.Attempt + 1), delay);
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
            AskMediathek();
        });

        Receive<QueryMediathekError>(failed =>
        {
            _log.Warning(failed.Cause, "Movie search {SearchId} mediathek query failed", _state.SearchId);
            Reply(new SearchMovieFailed(_state.SearchId, failed.Cause));
        });
    }

    private void Evaluating()
    {
        Receive<EvaluateMoviesCompleted>(evaluation => Reply(_state.ToSearchCompleted(evaluation)));

        Receive<EvaluateMoviesFailed>(failed =>
        {
            _log.Warning(failed.Cause, "Movie search {SearchId} evaluation failed", _state.SearchId);
            Reply(new SearchMovieFailed(_state.SearchId, failed.Cause));
        });
    }

    private void QueryMediathek()
    {
        _state.Attempt = 0;
        AskMediathek();
        Become(Querying);
    }

    private void AskMediathek()
    {
        var queries = _state.GetMediathekQueries();
        Task.WhenAll(queries.Select(q => _mediathekManager.Ask<QueryMediathekResponse>(q, _mediathekTimeout)))
            .PipeTo(Self,
                success: responses => MediathekQueryMerge.Merge(responses),
                failure: ex => new QueryMediathekError(ex));
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
