using FunkArr.ArrApi.Sabnzbd.Services;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

internal static class SabnzbdResultAssert
{
    internal static SabnzbdResult<T>.Ok Ok<T>(SabnzbdResult<T> result) =>
        Assert.IsType<SabnzbdResult<T>.Ok>(result);

    internal static SabnzbdResult<T>.Error Error<T>(SabnzbdResult<T> result) =>
        Assert.IsType<SabnzbdResult<T>.Error>(result);
}
