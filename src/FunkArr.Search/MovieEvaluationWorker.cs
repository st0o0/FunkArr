using System.Diagnostics;
using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class MovieEvaluationWorker : ReceiveActor
{
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _scoringTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _enrichmentTimeout = TimeSpan.FromSeconds(10);

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly MovieEvaluationState _state;

    private readonly IActorRef _scoringManager = Context.GetActor<IScoringManager>();
    private readonly IActorRef _ruleSetResolver = Context.GetActor<IRuleSetResolver>();
    private readonly IActorRef _enrichmentManager = Context.GetActor<IEnrichmentManager>();
    private readonly IActorRef _historyRegion = Context.GetActor<IHistoryRegion>();

    private readonly Stopwatch _stopwatch = new();

    public MovieEvaluationWorker(string nttId, IOptionsMonitor<VariantOptions>? variantOptions = null)
    {
        _state = MovieEvaluationState.From(nttId, variantOptions?.CurrentValue);

        Receive<EvaluateMovies>(cmd =>
        {
            _log.Debug("Movie evaluation {SearchId} started: Source={Source}, Items={Count}",
                cmd.SearchId, cmd.Source, cmd.Items.Length);
            _stopwatch.Restart();
            _state.Init(cmd, Sender);

            if (_state.TryGetRuleSetRequest(out var request))
            {
                _ruleSetResolver.Ask<ResolveRuleSetResponse>(request, _ruleSetTimeout)
                    .PipeTo(Self, failure: ex => new RuleSetFailed(ex));
                Become(ResolvingRuleSet);
                return;
            }

            ScoreOrReply();
        });
    }

    private void ResolvingRuleSet()
    {
        Receive<RuleSetResolved>(resolved =>
        {
            _state.Apply(resolved.RuleSetId, resolved.MediaName, resolved.Enrichment);
            ScoreOrReply();
        });

        Receive<RuleSetFailed>(_ => Reply(_state.ToCompleted()));
    }

    private void Scoring()
    {
        Receive<ScoreCompleted>(scored =>
        {
            _state.Apply(scored);

            if (_state.TryGetEnrichmentRequest(out var enrichRequest))
            {
                _enrichmentManager.Ask<EnrichMoviesResponse>(enrichRequest, _enrichmentTimeout)
                    .PipeTo(Self, failure: ex => new EnrichMoviesFailed(ex));
                Become(Enriching);
                return;
            }

            RecordHistory();
            Reply(_state.ToCompleted());
        });

        Receive<ScoringFailed>(failed =>
        {
            _log.Warning(failed.Cause, "Movie evaluation {SearchId} scoring failed", _state.SearchId);
            Reply(new EvaluateMoviesFailed(_state.SearchId, failed.Cause));
        });
    }

    private void Enriching()
    {
        Receive<EnrichMoviesCompleted>(enriched =>
        {
            _state.Apply(enriched);
            _state.MergeEnrichmentIntoTraces(enriched.Movies);
            RecordHistory();
            Reply(_state.ToCompleted());
        });

        Receive<EnrichMoviesFailed>(_ =>
        {
            RecordHistory();
            Reply(_state.ToCompleted());
        });
    }

    private void ScoreOrReply()
    {
        if (_state.TryGetScoringRequest(out var scoringRequest))
        {
            _scoringManager.Ask<ScoreItemsResponse>(scoringRequest, _scoringTimeout)
                .PipeTo(Self, failure: ex => new ScoringFailed(ex));
            Become(Scoring);
            return;
        }

        Reply(_state.ToCompleted());
    }

    private void RecordHistory()
    {
        var record = _state.BuildRecordHistory();
        if (record is not null)
        {
            _historyRegion.Tell(record);
        }
    }

    private void Reply(EvaluateMoviesResponse response)
    {
        _stopwatch.Stop();
        var sourceTag = new KeyValuePair<string, object?>("source", _state.Source.ToString().ToLowerInvariant());
        var typeTag = new KeyValuePair<string, object?>("type", "movie");
        var statusTag = new KeyValuePair<string, object?>("status", response is EvaluateMoviesCompleted ? "completed" : "failed");
        Telemetry.Evaluations.Add(1, sourceTag, typeTag, statusTag);
        Telemetry.EvaluationDuration.Record(_stopwatch.Elapsed.TotalSeconds, sourceTag, typeTag);

        _state.ReplyTo.Tell(response);
    }
}
