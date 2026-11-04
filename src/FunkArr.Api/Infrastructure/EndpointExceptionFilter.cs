using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FunkArr.Api.Infrastructure;

internal sealed class EndpointExceptionFilter(ILogger<EndpointExceptionFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(
                title: "Bad Request",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (TimeoutException ex)
        {
            var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
            logger.LogWarning(ex, "Timeout in endpoint {Endpoint}", endpoint);
            return ApiResults.GatewayTimeout();
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (UnexpectedActorResponseException ex)
        {
            var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
            logger.LogError("Unexpected actor response {ResponseType} in endpoint {Endpoint}", ex.ResponseType, endpoint);
            return ApiResults.InternalServerError();
        }
        catch (Exception ex)
        {
            var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
            logger.LogError(ex, "Unhandled exception in endpoint {Endpoint}", endpoint);
            return ApiResults.InternalServerError();
        }
    }
}
