using FunkArr.ArrApi.Newznab.Models;

namespace FunkArr.ArrApi.Newznab;

public abstract record SearchServiceResult
{
    public sealed record Success(Rss Rss) : SearchServiceResult;
    public sealed record Empty(int Offset) : SearchServiceResult;
    public sealed record Failed(string Message) : SearchServiceResult;
}
