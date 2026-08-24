namespace FunkArr.Shared;

public interface IShardedMessage
{
    string EntityKey { get; }
}
