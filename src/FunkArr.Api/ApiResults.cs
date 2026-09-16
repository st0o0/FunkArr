using Microsoft.AspNetCore.Http;

namespace FunkArr.Api;

internal static class ApiResults
{
    internal static IResult GatewayTimeout() =>
        Results.Problem(statusCode: 504, title: "Gateway Timeout");
}
