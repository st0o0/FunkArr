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

public sealed class TvSearchWorker : ReceiveActor, IWithTimers
{
    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(40);

    private static readonly BackoffPolicy _retryBackoff =
        Backoff.Create(TimeSpan.FromMilliseconds(500), multiplier: 3.0, maxDelay: TimeSpan.FromMilliseconds(1500));

    private const int MaxRetries = 2;

    private sealed record RetryMediathekQuery(int Attempt);

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly TvSearchWorkerState _state;

    private readonly IActorRef _mediathekManager = Context.GetActor<IMediathekManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _evaluationRegion = Context.GetActor<ITvEvaluationRegion>();

    private readonly Stopwatch _stopwatch = new();

    public ITimerScheduler Timers { get; set; } = null!;

    public TvSearchWorker(string nttId)
    {
        _state = TvSearchWorkerState.From(nttId);

        Receive<SearchSeries>(cmd =>
        {
            _log.Info("TV search started: Query={Query}, TvdbId={TvdbId}", cmd.Query, cmd.TvdbId);
            _stopwatch.Restart();
            _state.Init(cmd, Sender);

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

            _evaluationRegion.Ask<EvaluateSeriesResponse>(_state.ToEvaluation(Guid.NewGuid()), _evaluationTimeout)
                .PipeTo(Self, failure: ex => new EvaluateSeriesFailed(_state.SearchId, ex));
            Become(Evaluating);
        });

        Receive<QueryMediathekQueueFull>(_ =>
        {
            if (_state.Attempt < MaxRetries)
            {
                var delay = _retryBackoff.DelayWithJitter(_state.Attempt);
                _log.Info("TV search {SearchId} queue full, retry {Attempt} in {Delay}ms",
                    _state.SearchId, _state.Attempt + 1, delay.TotalMilliseconds);
                Timers.StartSingleTimer("mediathek-retry", new RetryMediathekQuery(_state.Attempt + 1), delay);
            }
            else
            {
                _log.Warning("TV search {SearchId} queue full, retries exhausted", _state.SearchId);
                Telemetry.Timeouts.Add(1);
                Reply(new SearchSeriesFailed(_state.SearchId,
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
            _log.Warning(failed.Cause, "TV search {SearchId} mediathek query failed", _state.SearchId);
            Reply(new SearchSeriesFailed(_state.SearchId, failed.Cause));
        });
    }

    private void Evaluating()
    {
        Receive<EvaluateSeriesCompleted>(evaluation => Reply(_state.ToSearchCompleted(evaluation)));

        Receive<EvaluateSeriesFailed>(failed =>
        {
            _log.Warning(failed.Cause, "TV search {SearchId} evaluation failed", _state.SearchId);
            Reply(new SearchSeriesFailed(_state.SearchId, failed.Cause));
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
        _mediathekManager.Ask<QueryMediathekResponse>(_state.GetMediathekQuery(), _mediathekTimeout)
            .PipeTo(Self, failure: ex => new QueryMediathekError(ex));
    }

    private void Reply(SearchSeriesResponse response)
    {
        _stopwatch.Stop();
        var sourceTag = new KeyValuePair<string, object?>("source", _state.Source.ToString().ToLowerInvariant());
        var typeTag = new KeyValuePair<string, object?>("type", "tv");
        Telemetry.SearchRequests.Add(1, sourceTag);
        Telemetry.SearchDuration.Record(_stopwatch.Elapsed.TotalSeconds, sourceTag, typeTag);
        if (response is SearchSeriesCompleted completed)
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
