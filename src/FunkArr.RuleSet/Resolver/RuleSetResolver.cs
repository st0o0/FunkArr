using Akka.Actor;
using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet.Resolver;

public sealed class RuleSetResolver : ReceiveActor
{
    private readonly RuleSetResolverState _state = new();

    public RuleSetResolver()
    {
        Receive<RegisterRuleSet>(msg => _state.Apply(msg));
        Receive<DeregisterRuleSet>(msg => _state.Apply(msg));
        Receive<ResolveRuleSet>(msg => Sender.Tell(_state.Resolve(msg)));
        Receive<QueryRegisteredRuleSets>(_ => Sender.Tell(_state.QueryAll()));
    }
}
