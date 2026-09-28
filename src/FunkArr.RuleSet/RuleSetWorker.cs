using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.RuleSet.DiskModel;
using Servus.Akka;

namespace FunkArr.RuleSet;

public sealed class RuleSetWorker : ReceiveActor
{
    public sealed record LoadRuleSet(
        string RuleSetId,
        string? CommunityPath,
        string? LocalPath) : IWithRuleSetId;

    public sealed record RemoveRuleSet(string RuleSetId) : IWithRuleSetId;

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string _nttId;
    private readonly RuleSetStore _store;
    private readonly IRuleSetValidator _validator;
    private DiskRuleSet? _merged;
    private RuleSetPaths _paths = new(null, null, null, null);

    public RuleSetWorker(string nttId, RuleSetStore store, IRuleSetValidator validator)
    {
        _nttId = nttId;
        _store = store;
        _validator = validator;

        Receive<LoadRuleSet>(HandleLoad);
        Receive<RemoveRuleSet>(HandleRemove);
        Receive<QueryRuleSetDetail>(HandleQueryDetail);
        Receive<CreateLocalRuleSet>(HandleCreate);
        Receive<UpdateLocalRuleSet>(HandleUpdate);
        Receive<DeleteLocalRuleSet>(HandleDelete);
        Receive<ExportRuleSet>(HandleExport);
    }

    private void HandleLoad(LoadRuleSet msg)
    {
        _merged = _store.LoadMerged(_nttId);
        _paths = _store.CheckPaths(_nttId);

        if (_merged is null)
        {
            _log.Warning("RuleSet '{RuleSetId}': no valid JSON found", _nttId);
            return;
        }

        var identity = _merged.ToIdentity();
        if (identity is null)
        {
            return;
        }

        var config = _merged.ToMatchingConfig(_nttId);
        if (config is not null)
        {
            var scoringManager = Context.GetActor<IScoringManager>();
            scoringManager.Tell(config);
        }

        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Tell(new RegisterRuleSet(
            _nttId, identity.Value.Topic, identity.Value.Aliases,
            identity.Value.Ids,
            identity.Value.MediaName, identity.Value.MediaType, identity.Value.Enrichment));

        var sourceType = (_paths.CommunityPath, _paths.LocalPath) switch
        {
            (not null, not null) => "merged",
            (not null, null) => "community",
            (null, not null) => "local",
            _ => "unknown"
        };

        var manager = Context.GetActor<IRuleSetManager>();
        manager.Tell(new WorkerReady(_nttId, config?.Rules.Length ?? 0, sourceType));
        Telemetry.Loaded.Add(1, new KeyValuePair<string, object?>("source", sourceType));
    }

    private void HandleRemove(RemoveRuleSet msg)
    {
        _merged = null;

        var scoringManager = Context.GetActor<IScoringManager>();
        scoringManager.Tell(new RemoveMatchingConfig(_nttId));

        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Tell(new DeregisterRuleSet(_nttId));

        var manager = Context.GetActor<IRuleSetManager>();
        manager.Tell(new WorkerRemoved(_nttId));
        Telemetry.Removed.Add(1);
    }

    private void HandleQueryDetail(QueryRuleSetDetail msg)
    {
        if (_merged is null)
        {
            _merged = _store.LoadMerged(_nttId);
            _paths = _store.CheckPaths(_nttId);
        }

        if (_merged is null)
        {
            Sender.Tell(new RuleSetDetailFailed(new RuleSetNotFoundException(_nttId)));
            return;
        }

        var identity = _merged.ToIdentity();
        if (identity is null)
        {
            Sender.Tell(new RuleSetDetailFailed(new RuleSetNotFoundException(_nttId)));
            return;
        }

        var config = _merged.ToMatchingConfig(_nttId);

        Sender.Tell(new RuleSetDetailResult(
            _nttId,
            new RuleSetDetailResult.RuleSetIdentity(
                identity.Value.Topic, identity.Value.Aliases,
                identity.Value.Ids),
            new RuleSetDetailResult.RuleSetSource(
                _paths.CommunityPath, _paths.LocalPath,
                _paths.CommunityModified, _paths.LocalModified),
            config?.DefaultConfidence ?? 0f,
            _merged.ToDetailRules(),
            identity.Value.Enrichment));
    }

    private void HandleCreate(CreateLocalRuleSet msg)
    {
        if (_store.ExistsLocal(_nttId))
        {
            Sender.Tell(new CreateLocalRuleSetFailed(CreateLocalRuleSetFailureReason.AlreadyExists));
            return;
        }

        var disk = msg.Body.ToDiskRuleSet();
        var json = _store.SaveLocal(_nttId, disk);
        var errors = _validator.Validate(json);
        if (errors.Count > 0)
        {
            _store.DeleteLocal(_nttId);
            Telemetry.ValidationErrors.Add(1);
            Sender.Tell(new CreateLocalRuleSetValidationFailed([.. errors]));
            return;
        }

        Self.Tell(new LoadRuleSet(_nttId, null, null));
        Sender.Tell(new CreateLocalRuleSetCompleted(_nttId));
    }

    private void HandleUpdate(UpdateLocalRuleSet msg)
    {
        if (!_store.ExistsLocal(_nttId) && !_store.ExistsCommunity(_nttId))
        {
            Sender.Tell(new UpdateLocalRuleSetFailed(UpdateLocalRuleSetFailureReason.NotFound));
            return;
        }

        var disk = msg.Body.ToDiskRuleSet();
        var json = _store.SaveLocal(_nttId, disk);
        var errors = _validator.Validate(json);
        if (errors.Count > 0)
        {
            _store.DeleteLocal(_nttId);
            Telemetry.ValidationErrors.Add(1);
            Sender.Tell(new UpdateLocalRuleSetValidationFailed([.. errors]));
            return;
        }

        Self.Tell(new LoadRuleSet(_nttId, null, null));
        Sender.Tell(new UpdateLocalRuleSetCompleted());
    }

    private void HandleDelete(DeleteLocalRuleSet msg)
    {
        if (!_store.DeleteLocal(_nttId))
        {
            Sender.Tell(new DeleteLocalRuleSetFailed(DeleteLocalRuleSetFailureReason.NotFound));
            return;
        }

        if (_store.ExistsCommunity(_nttId))
        {
            Self.Tell(new LoadRuleSet(_nttId, null, null));
        }
        else
        {
            _merged = null;
            var scoringManager = Context.GetActor<IScoringManager>();
            scoringManager.Tell(new RemoveMatchingConfig(_nttId));

            var resolver = Context.GetActor<IRuleSetResolver>();
            resolver.Tell(new DeregisterRuleSet(_nttId));

            var manager = Context.GetActor<IRuleSetManager>();
            manager.Tell(new WorkerRemoved(_nttId));
        }

        Sender.Tell(new DeleteLocalRuleSetCompleted());
    }

    private void HandleExport(ExportRuleSet msg)
    {
        if (!_store.ExistsLocal(_nttId))
        {
            Sender.Tell(new ExportRuleSetFailed(ExportRuleSetFailureReason.NoLocalOverlay));
            return;
        }

        var json = _store.ExportMergedJson(_nttId);
        if (json is null)
        {
            Sender.Tell(new ExportRuleSetFailed(ExportRuleSetFailureReason.NotFound));
            return;
        }

        var errors = _validator.Validate(json);
        if (errors.Count > 0)
        {
            Telemetry.ValidationErrors.Add(1);
            Sender.Tell(new ExportRuleSetValidationFailed([.. errors]));
            return;
        }

        Sender.Tell(new ExportRuleSetCompleted(json));
    }
}
