using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.ArrApi;

internal sealed class ApiKeyEndpointFilter(Func<IResult> errorFactory) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedApiKey = configuration["FunkArr:ApiKey"] ?? "funkarr-default-api-key";
        var apiKey = context.HttpContext.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || !string.Equals(apiKey, expectedApiKey, StringComparison.Ordinal))
        {
            return errorFactory();
        }

        return await next(context);
    }
}
