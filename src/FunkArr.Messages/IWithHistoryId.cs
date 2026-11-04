using Terp.Akka;

namespace FunkArr.Messages;

public interface IWithHistoryId : IWithEntityId
{
    string HistoryId { get; }
    string IWithEntityId.EntityId => HistoryId;
}
