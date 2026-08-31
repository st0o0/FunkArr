using Akka.Actor;
using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet;

public sealed class RuleSetResolver : ReceiveActor
{
    private sealed record State(
        Dictionary<string, string> LookupIndex,
        Dictionary<string, HashSet<string>> EntriesByRuleSetId);

    private State _state = new(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));

    public RuleSetResolver()
    {
        Receive<RegisterRuleSet>(HandleRegister);
        Receive<ResolveRuleSet>(HandleResolve);
    }

    private void HandleRegister(RegisterRuleSet msg)
    {
        if (_state.EntriesByRuleSetId.TryGetValue(msg.RuleSetId, out var previousKeys))
        {
            foreach (var key in previousKeys)
            {
                _state.LookupIndex.Remove(key);
            }
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { msg.Topic };
        _state.LookupIndex[msg.Topic] = msg.RuleSetId;

        foreach (var alias in msg.Aliases)
        {
            _state.LookupIndex[alias] = msg.RuleSetId;
            keys.Add(alias);
        }

        _state.EntriesByRuleSetId[msg.RuleSetId] = keys;
    }

    private void HandleResolve(ResolveRuleSet msg)
    {
        if (_state.LookupIndex.TryGetValue(msg.TopicOrAlias, out var ruleSetId))
        {
            Sender.Tell(new RuleSetResolved(ruleSetId));
        }
        else
        {
            Sender.Tell(new RuleSetNotFound(msg.TopicOrAlias));
        }
    }
}
