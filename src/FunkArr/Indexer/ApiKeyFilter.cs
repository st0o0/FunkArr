using Microsoft.Extensions.Options;
using FunkArr.Configuration;

namespace FunkArr.Indexer;

public sealed class ApiKeyFilter(IOptions<FunkArrOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var apiKey = httpContext.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || apiKey != options.Value.ApiKey)
        {
            return Results.Content(
                NewznabXmlBuilder.BuildErrorResponse(100, "Incorrect user credentials"),
                "application/xml");
        }

        return await next(context);
    }
}
