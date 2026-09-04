using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
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
    private readonly IDataFiles _dataFiles;

    public RuleSetWorker(IDataFiles dataFiles)
    {
        _dataFiles = dataFiles;

        Receive<LoadRuleSet>(HandleLoad);
        Receive<RemoveRuleSet>(HandleRemove);
    }

    private void HandleLoad(LoadRuleSet msg)
    {
        var communityJson = msg.CommunityPath is not null && _dataFiles.Exists(msg.CommunityPath)
            ? _dataFiles.ReadText(msg.CommunityPath)
            : null;

        var localJson = msg.LocalPath is not null && _dataFiles.Exists(msg.LocalPath)
            ? _dataFiles.ReadText(msg.LocalPath)
            : null;

        var identity = RuleSetMerger.ExtractIdentity(communityJson, localJson);
        if (identity is null)
        {
            _log.Warning("RuleSet '{RuleSetId}': no valid JSON found", msg.RuleSetId);
            return;
        }

        var config = RuleSetMerger.Build(msg.RuleSetId, communityJson, localJson);
        if (config is null)
        {
            _log.Warning("RuleSet '{RuleSetId}': merge produced no config", msg.RuleSetId);
            return;
        }

        var matchMagicManager = Context.GetActor<IMatchMagicManager>();
        matchMagicManager.Tell(config);

        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Tell(new RegisterRuleSet(
            msg.RuleSetId, identity.Value.Topic, identity.Value.Aliases,
            identity.Value.TvdbId, identity.Value.ImdbId, identity.Value.TmdbId));
    }

    private void HandleRemove(RemoveRuleSet msg)
    {
        var matchMagicManager = Context.GetActor<IMatchMagicManager>();
        matchMagicManager.Tell(new RemoveMatchingConfig(msg.RuleSetId));

        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Tell(new DeregisterRuleSet(msg.RuleSetId));
    }
}
