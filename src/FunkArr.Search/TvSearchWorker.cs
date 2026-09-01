using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class TvSearchWorker : ReceiveActor
{
    private TvSearchWorkerState? _state;

    public TvSearchWorker()
    {
        var mediathekManager = Context.GetActor<IMediathekManager>();
        var matchMagicManager = Context.GetActor<IMatchMagicManager>();
        var ruleSetResolver = Context.GetActor<IRuleSetResolver>();

        Receive<TvSearchCommand>(cmd =>
        {
            _state = TvSearchWorkerState.Empty.Apply(cmd);

            var fields = new List<MediathekQueryField>();
            if (!string.IsNullOrWhiteSpace(cmd.Query))
            {
                fields.Add(new MediathekQueryField(["topic"], cmd.Query));
            }

            var query = new MediathekQuery(
                Fields: fields.ToArray(),
                SortBy: "timestamp",
                SortOrder: "desc",
                Future: false,
                Offset: cmd.Offset ?? 0,
                Size: cmd.Limit ?? 50,
                DurationMin: 300,
                DurationMax: null);

            mediathekManager.Ask<object>(query, TimeSpan.FromSeconds(15))
                .PipeTo(Self, Sender);
        });

        Receive<MediathekQueryCompleted>(result =>
        {
            if (_state is null) return;

            _state = _state.Apply(result);

            var topic = result.Items.Length > 0 ? result.Items[0].Topic : null;
            if (topic is not null)
            {
                ruleSetResolver.Ask<object>(new ResolveRuleSet(topic), TimeSpan.FromSeconds(5))
                    .PipeTo(Self, Sender);
            }
            else
            {
                Sender.Tell(_state.ToUnscoredResult());
                Context.Stop(Self);
            }
        });

        Receive<RuleSetResolved>(resolved =>
        {
            if (_state is null) return;

            _state = _state.ApplyRuleSet(resolved.RuleSetId);

            var candidates = _state.RawItems.Select(item => new ScoreCandidate(
                item.Title, item.Topic, item.Channel, item.Duration,
                TvSearchWorkerStateExtensions.ResolveQuality(item),
                item.Description, item.Timestamp)).ToArray();

            var requestId = Guid.NewGuid();
            var origin = new ScoringOrigin("sonarr", _state.RawItems[0].Topic);
            matchMagicManager.Ask<object>(
                    new ScoreItems(requestId, resolved.RuleSetId, origin, candidates),
                    TimeSpan.FromSeconds(10))
                .PipeTo(Self, Sender);
        });

        Receive<RuleSetNotFound>(_ =>
        {
            if (_state is null) return;
            Sender.Tell(_state.ToUnscoredResult());
            Context.Stop(Self);
        });

        Receive<ScoreCompleted>(scored =>
        {
            if (_state is null) return;
            Sender.Tell(_state.ToScoredResult(scored));
            Context.Stop(Self);
        });

        Receive<MediathekQueryFailed>(failed =>
        {
            if (_state is null) return;
            Sender.Tell(new SearchFailed(_state.SearchId, failed.Reason));
            Context.Stop(Self);
        });

        Receive<Status.Failure>(failure =>
        {
            if (_state is null) return;
            Sender.Tell(new SearchFailed(_state.SearchId, failure.Cause.Message));
            Context.Stop(Self);
        });
    }
}
