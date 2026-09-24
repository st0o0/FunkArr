using System.Text.Json;
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
    private readonly RuleSetStore _store;
    private readonly IRuleSetValidator _validator;
    private DiskRuleSet? _merged;
    private RuleSetPaths _paths = new(null, null, null, null);

    public RuleSetWorker(RuleSetStore store, IRuleSetValidator validator)
    {
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

    private string RuleSetId => Self.Path.Name;

    private void HandleLoad(LoadRuleSet msg)
    {
        _merged = _store.LoadMerged(msg.RuleSetId);
        _paths = _store.CheckPaths(msg.RuleSetId);

        if (_merged is null)
        {
            _log.Warning("RuleSet '{RuleSetId}': no valid JSON found", msg.RuleSetId);
            return;
        }

        var identity = _merged.ToIdentity();
        if (identity is null)
        {
            return;
        }

        var config = _merged.ToMatchingConfig(msg.RuleSetId);
        if (config is not null)
        {
            var scoringManager = Context.GetActor<IScoringManager>();
            scoringManager.Tell(config);
        }

        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Tell(new RegisterRuleSet(
            msg.RuleSetId, identity.Value.Topic, identity.Value.Aliases,
            identity.Value.TvdbId, identity.Value.ImdbId, identity.Value.TmdbId,
            identity.Value.MediaName, identity.Value.MediaType, identity.Value.Enrichment));

        var sourceType = (_paths.CommunityPath, _paths.LocalPath) switch
        {
            (not null, not null) => "merged",
            (not null, null) => "community",
            (null, not null) => "local",
            _ => "unknown",
        };

        var manager = Context.GetActor<IRuleSetManager>();
        manager.Tell(new WorkerReady(msg.RuleSetId, config?.Rules.Length ?? 0, sourceType));
    }

    private void HandleRemove(RemoveRuleSet msg)
    {
        _merged = null;

        var scoringManager = Context.GetActor<IScoringManager>();
        scoringManager.Tell(new RemoveMatchingConfig(msg.RuleSetId));

        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Tell(new DeregisterRuleSet(msg.RuleSetId));

        var manager = Context.GetActor<IRuleSetManager>();
        manager.Tell(new WorkerRemoved(msg.RuleSetId));
    }

    private void HandleQueryDetail(QueryRuleSetDetail msg)
    {
        if (_merged is null)
        {
            _merged = _store.LoadMerged(msg.RuleSetId);
            _paths = _store.CheckPaths(msg.RuleSetId);
        }

        if (_merged is null)
        {
            Sender.Tell(new RuleSetDetailFailed(new RuleSetNotFoundException(msg.RuleSetId)));
            return;
        }

        var identity = _merged.ToIdentity();
        if (identity is null)
        {
            Sender.Tell(new RuleSetDetailFailed(new RuleSetNotFoundException(msg.RuleSetId)));
            return;
        }

        var config = _merged.ToMatchingConfig(msg.RuleSetId);

        Sender.Tell(new RuleSetDetailResult(
            msg.RuleSetId,
            new RuleSetDetailResult.RuleSetIdentity(
                identity.Value.Topic, identity.Value.Aliases,
                identity.Value.TvdbId, identity.Value.ImdbId, identity.Value.TmdbId),
            new RuleSetDetailResult.RuleSetSource(
                _paths.CommunityPath, _paths.LocalPath,
                _paths.CommunityModified, _paths.LocalModified),
            config?.DefaultConfidence ?? 0f,
            _merged.ToDetailRules(),
            identity.Value.Enrichment));
    }

    private void HandleCreate(CreateLocalRuleSet msg)
    {
        if (_store.ExistsLocal(msg.RuleSetId))
        {
            Sender.Tell(new CreateLocalRuleSetFailed(CreateLocalRuleSetFailureReason.AlreadyExists));
            return;
        }

        var disk = msg.Body.ToDiskRuleSet();
        var json = _store.SaveLocal(msg.RuleSetId, disk);
        var errors = _validator.Validate(json);
        if (errors.Count > 0)
        {
            _store.DeleteLocal(msg.RuleSetId);
            Sender.Tell(new CreateLocalRuleSetValidationFailed(errors.Select(e => e.Message).ToList()));
            return;
        }

        Self.Tell(new LoadRuleSet(msg.RuleSetId, null, null));
        Sender.Tell(new CreateLocalRuleSetCompleted(msg.RuleSetId));
    }

    private void HandleUpdate(UpdateLocalRuleSet msg)
    {
        if (!_store.ExistsLocal(msg.RuleSetId) && !_store.ExistsCommunity(msg.RuleSetId))
        {
            Sender.Tell(new UpdateLocalRuleSetFailed(UpdateLocalRuleSetFailureReason.NotFound));
            return;
        }

        var disk = msg.Body.ToDiskRuleSet();
        var json = _store.SaveLocal(msg.RuleSetId, disk);
        var errors = _validator.Validate(json);
        if (errors.Count > 0)
        {
            _store.DeleteLocal(msg.RuleSetId);
            Sender.Tell(new UpdateLocalRuleSetValidationFailed(errors.Select(e => e.Message).ToList()));
            return;
        }

        Self.Tell(new LoadRuleSet(msg.RuleSetId, null, null));
        Sender.Tell(new UpdateLocalRuleSetCompleted());
    }

    private void HandleDelete(DeleteLocalRuleSet msg)
    {
        if (!_store.DeleteLocal(msg.RuleSetId))
        {
            Sender.Tell(new DeleteLocalRuleSetFailed(DeleteLocalRuleSetFailureReason.NotFound));
            return;
        }

        if (_store.ExistsCommunity(msg.RuleSetId))
        {
            Self.Tell(new LoadRuleSet(msg.RuleSetId, null, null));
        }
        else
        {
            _merged = null;
            var scoringManager = Context.GetActor<IScoringManager>();
            scoringManager.Tell(new RemoveMatchingConfig(msg.RuleSetId));

            var resolver = Context.GetActor<IRuleSetResolver>();
            resolver.Tell(new DeregisterRuleSet(msg.RuleSetId));

            var manager = Context.GetActor<IRuleSetManager>();
            manager.Tell(new WorkerRemoved(msg.RuleSetId));
        }

        Sender.Tell(new DeleteLocalRuleSetCompleted());
    }

    private void HandleExport(ExportRuleSet msg)
    {
        if (!_store.ExistsLocal(msg.RuleSetId))
        {
            Sender.Tell(new ExportRuleSetFailed(ExportRuleSetFailureReason.NotFound));
            return;
        }

        var json = _store.ExportMergedJson(msg.RuleSetId);
        if (json is null)
        {
            Sender.Tell(new ExportRuleSetFailed(ExportRuleSetFailureReason.NotFound));
            return;
        }

        var errors = _validator.Validate(json);
        if (errors.Count > 0)
        {
            Sender.Tell(new ExportRuleSetValidationFailed(errors.Select(e => e.Message).ToList()));
            return;
        }

        Sender.Tell(new ExportRuleSetCompleted(json));
    }
}
