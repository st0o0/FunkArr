using Microsoft.AspNetCore.Http;

namespace FunkArr.ArrApi;

internal sealed class ApiKeyEndpointFilter(string expectedApiKey, Func<IResult> errorFactory) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var apiKey = context.HttpContext.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || !string.Equals(apiKey, expectedApiKey, StringComparison.Ordinal))
        {
            return errorFactory();
        }

        return await next(context);
    }
}
