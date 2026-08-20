using System.Text.Json;
using FunkArr.Configuration;
using FunkArr.Indexer;
using Microsoft.Extensions.Options;

namespace FunkArr.Api;

public sealed class ApiKeyMiddleware(RequestDelegate next, IOptions<FunkArrOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ShouldSkipAuth(path))
        {
            await next(context);
            return;
        }

        var apiKey = context.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || apiKey != options.Value.ApiKey)
        {
            if (IsNewznabRoute(path, context.Request.QueryString.Value))
            {
                context.Response.ContentType = "application/xml";
                await context.Response.WriteAsync(
                    NewznabXmlBuilder.BuildErrorResponse(100, "Incorrect user credentials"));
            }
            else
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body,
                    new { error = "Incorrect user credentials" }, JsonOptions);
            }

            return;
        }

        await next(context);
    }

    private static bool ShouldSkipAuth(string path)
    {
        if (path.Equals("/healthz", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/alive", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.Equals("/api/fake_nzb", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/download", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsNewznabRoute(string path, string? queryString)
    {
        return path.Equals("/api", StringComparison.OrdinalIgnoreCase) &&
               (queryString is null || !queryString.Contains("version", StringComparison.OrdinalIgnoreCase));
    }
}
