namespace FunkArr.ArrApi.Sabnzbd.Services;

public abstract record SabnzbdResult
{
    public sealed record Ok(object Data) : SabnzbdResult;
    public sealed record Error(string Message, int StatusCode = 400) : SabnzbdResult;
}
