using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FunkArr.Api;

internal sealed class EndpointExceptionFilter(ILogger<EndpointExceptionFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (TimeoutException ex)
        {
            var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
            logger.LogError(ex, "Timeout in endpoint {Endpoint}", endpoint);
            return ApiResults.GatewayTimeout();
        }
        catch (Exception ex)
        {
            var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
            logger.LogError(ex, "Unhandled exception in endpoint {Endpoint}", endpoint);
            return Results.Problem(
                title: "Internal Server Error",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}
