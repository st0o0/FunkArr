using Terp;
using Terp.Akka;

namespace FunkArr.Messages;

public interface IWithSearchId : IWithId, IWithEntityId
{
    Guid SearchId { get; }
    Guid IWithId.Id => SearchId;
    string IWithEntityId.EntityId => SearchId.ToString();
}
