using Akka.Actor;
using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet;

public sealed class RuleSetResolver : ReceiveActor
{
    private RuleSetResolverState _state = RuleSetResolverState.Empty;

    public RuleSetResolver()
    {
        Receive<RegisterRuleSet>(msg => _state = _state.Apply(msg));
        Receive<ResolveRuleSet>(msg => Sender.Tell(_state.Resolve(msg)));
    }
}
