using FunkArr.ArrApi;
using Microsoft.AspNetCore.HttpLogging;

namespace FunkArr.Configuration;

internal sealed class ApiKeyLoggingInterceptor : IHttpLoggingInterceptor
{
    public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
    {
        var query = logContext.HttpContext.Request.QueryString.Value;
        if (query is not null && query.Contains("apikey", StringComparison.OrdinalIgnoreCase))
        {
            logContext.AddParameter("QueryString", QueryStringRedactor.RedactApiKey(query));
            logContext.TryDisable(HttpLoggingFields.RequestQuery);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext) => ValueTask.CompletedTask;
}
