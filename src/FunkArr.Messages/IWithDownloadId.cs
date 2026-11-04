using Terp;
using Terp.Akka;

namespace FunkArr.Messages;

public interface IWithDownloadId : IWithId, IWithEntityId
{
    Guid DownloadId { get; }
    Guid IWithId.Id => DownloadId;
    string IWithEntityId.EntityId => DownloadId.ToString();
}
