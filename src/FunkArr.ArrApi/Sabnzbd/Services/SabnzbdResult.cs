namespace FunkArr.ArrApi.Sabnzbd.Services;

public sealed record SabnzbdError(string Message, int StatusCode = 400);

public abstract record SabnzbdResult<T>
{
    public sealed record Ok(T Data) : SabnzbdResult<T>;
    public sealed record Error(string Message, int StatusCode = 400) : SabnzbdResult<T>;

    public static implicit operator SabnzbdResult<T>(T data) => new Ok(data);
    public static implicit operator SabnzbdResult<T>(SabnzbdError error) => new Error(error.Message, error.StatusCode);
}
