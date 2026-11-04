using Microsoft.AspNetCore.Http;

namespace FunkArr.Api.Infrastructure;

internal static class ApiResults
{
    internal static IResult GatewayTimeout() =>
        Results.Problem(statusCode: 504, title: "Gateway Timeout");

    internal static IResult InternalServerError() =>
        Results.Problem(
            title: "Internal Server Error",
            detail: "An unexpected error occurred",
            statusCode: 500);

    internal static IResult UnexpectedResponse(object? response) =>
        throw new UnexpectedActorResponseException(response);
}

internal sealed class UnexpectedActorResponseException(object? response)
    : Exception($"Unexpected actor response: {response?.GetType().Name ?? "null"}")
{
    public string ResponseType { get; } = response?.GetType().Name ?? "null";
}
