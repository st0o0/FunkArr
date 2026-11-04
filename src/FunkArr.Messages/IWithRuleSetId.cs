using Terp.Akka;

namespace FunkArr.Messages;

public interface IWithRuleSetId : IWithEntityId
{
    string RuleSetId { get; }
    string IWithEntityId.EntityId => RuleSetId;
}
