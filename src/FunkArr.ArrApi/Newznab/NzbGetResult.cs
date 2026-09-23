using FunkArr.ArrApi.Newznab.Models;

namespace FunkArr.ArrApi.Newznab;

public abstract record NzbGetResult
{
    public sealed record Success(byte[] Content, string FileName) : NzbGetResult;
    public sealed record Error(NewznabError ErrorDetail) : NzbGetResult;
}
