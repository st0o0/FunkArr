using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.RuleSet;
using Servus.Akka;

namespace FunkArr.RuleSet;

public sealed class RuleSetWorker : ReceiveActor
{
    public sealed record InitializeRuleSet(
        string RuleSetId,
        string? CommunityPath,
        string? LocalPath) : IWithRuleSetId;

    private readonly ILoggingAdapter _log = Context.GetLogger();

    public RuleSetWorker()
    {
        Receive<InitializeRuleSet>(HandleInitialize);
    }

    private void HandleInitialize(InitializeRuleSet msg)
    {
        var communityJson = msg.CommunityPath is not null && File.Exists(msg.CommunityPath)
            ? File.ReadAllText(msg.CommunityPath)
            : null;

        var localJson = msg.LocalPath is not null && File.Exists(msg.LocalPath)
            ? File.ReadAllText(msg.LocalPath)
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
        resolver.Tell(new RegisterRuleSet(msg.RuleSetId, identity.Value.Topic, identity.Value.Aliases));
    }
}
