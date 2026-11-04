using Terp;
using Terp.Akka;

namespace FunkArr.Messages;

public interface IWithTestScoringId : IWithId, IWithEntityId
{
    Guid TestScoringId { get; }
    Guid IWithId.Id => TestScoringId;
    string IWithEntityId.EntityId => TestScoringId.ToString();
}
